using EAappEmulater.Enums;
using EAappEmulater.Models;
using EAappEmulater.Core;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace EAappEmulater.Helper;

public static class DownloadHelper
{
    private static double _globalLimitBps = 0;
    private static readonly HttpClient _httpClient = new();

    private static readonly ConcurrentQueue<DownloadTaskModel> _queue = new();
    private static DownloadTaskModel _active;
    private static readonly object _lock = new();

    private static readonly string _configPath = Path.Combine(Utils.CoreUtil.Dir_Config, "Config.ini");
    private const string DOWNLOAD_SECTION = "Downloads";

    private static readonly ConcurrentDictionary<string, bool> _running = new();

    public static event Action<DownloadTaskModel> ActiveTaskChanged;

    public static IEnumerable<DownloadTaskModel> GetQueue() => _queue.ToArray();
    public static DownloadTaskModel? GetActive() => _active;

    public static void SetGlobalLimitMbps(double? mbps)
    {
        _globalLimitBps = (mbps.HasValue && mbps.Value > 0) ? mbps.Value * 1024d * 1024d : 0;
        foreach (var kv in _limiters) kv.Value.SetLimit(_globalLimitBps);
    }

    public static void Enqueue(DownloadTaskModel task, bool autoStart = true)
    {
        lock (_lock)
        {
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.EnqueueTask", task.OfferId, task.GameName));
            _queue.Enqueue(task);
            PersistTask(task);
            
            // 通知用户游戏已加入下载队列
            NotifierHelper.Success(I18nHelper.I18n._("Helper.DownloadHelper.EnqueueSuccess", task.GameName));
            
            if (autoStart) TryStartNext();
        }
    }

    public static void ResumePersistedPausedTasks()
    {
        var tasks = ReadAllTasks();
        lock (_lock)
        {
            foreach (var t in tasks)
            {
                t.Status = DownloadStatus.Paused;
                _queue.Enqueue(t);
            }
        }
    }

    public static void Pause(DownloadTaskModel task)
    {
        LoggerHelper.Info($"Pause called for {task.OfferId}, current status {task.Status}");
        task.Pause();
        PersistTask(task); // 持久化暂停状态
    }

    public static void Resume(DownloadTaskModel task)
    {
        LoggerHelper.Info($"Resume called for {task.OfferId}, current status: {task.Status}");
        task.Resume();
        LoggerHelper.Info($"_running flag for {task.OfferId} before resume: {_running.GetValueOrDefault(task.OfferId)}");
        lock (_lock)
        {
            LoggerHelper.Info($"Checking if task is active: {_active?.OfferId}, task: {task.OfferId}, match: {_active == task}");
            // 如果任务是当前激活任务并且没有在运行，重新启动
            if (_active == task)
            {
                if (!_running.GetValueOrDefault(task.OfferId))
                {
                    LoggerHelper.Info($"Resuming active task {task.OfferId}");
                    task.Status = DownloadStatus.Waiting;
                    _ = RunDownloadAsync(task);
                    ActiveTaskChanged?.Invoke(task);
                }
                else
                {
                    LoggerHelper.Info($"Active task {task.OfferId} is still running; resume will take effect when it stops.");
                }
                return;
            }
            // 否则尝试从队列启动
            LoggerHelper.Info($"Task not active, trying TryStartNext");
            TryStartNext();
        }
    }

    public static void Cancel(DownloadTaskModel task)
    {
        task.Cancel();
        // 首先删除持久化
        try
        {
            IniHelper.DeleteKey(DOWNLOAD_SECTION, task.OfferId, _configPath);
        }
        catch (Exception ex) { LoggerHelper.Warn("DeletePersisted error: " + ex.Message); }

        // 删除zip文件
        try
        {
            var zipFile = Path.Combine(task.InstallDir ?? string.Empty, task.OfferId + ".zip");
            if (File.Exists(zipFile)) File.Delete(zipFile);
        }
        catch (Exception ex) { LoggerHelper.Warn("Delete zip file failed: " + ex.Message); }

        // 从队列移除
        lock (_lock)
        {
            RemoveFromQueue(task.OfferId);
            if (_active == task)
            {
                // 确保状态为Cancelled
                task.Status = DownloadStatus.Cancelled;
                
                // 通知UI任务已取消（状态为Cancelled）
                ActiveTaskChanged?.Invoke(task);
                
                // 然后清空激活任务
                _active = null;
                // 标记未运行
                _running[task.OfferId] = false;
                
                // 尝试启动下一个
                TryStartNext();
            }
        }

        // 清理配置文件中的残留项目
        try
        {
            if (File.Exists(_configPath))
            {
                var lines = File.ReadAllLines(_configPath).ToList();
                bool changed = false;
                bool inSection = false;
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    var line = lines[i];
                    if (line.Trim().StartsWith("[") && line.Trim().EndsWith("]"))
                    {
                        if (string.Equals(line.Trim(), "[" + DOWNLOAD_SECTION + "]", StringComparison.OrdinalIgnoreCase))
                        {
                            inSection = true;
                        }
                        else if (inSection)
                        {
                            inSection = false;
                        }
                        continue;
                    }
                    if (!inSection) continue;
                    var idx = line.IndexOf('=');
                    if (idx > 0)
                    {
                        var k = line.Substring(0, idx).Trim();
                        if (string.Equals(k, task.OfferId, StringComparison.OrdinalIgnoreCase))
                        {
                            lines.RemoveAt(i);
                            changed = true;
                        }
                    }
                }
                if (changed)
                {
                    bool foundSection = false;
                    bool hasKeys = false;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        var line = lines[i];
                        if (line.Trim().StartsWith("[") && line.Trim().EndsWith("]"))
                        {
                            if (string.Equals(line.Trim(), "[" + DOWNLOAD_SECTION + "]", StringComparison.OrdinalIgnoreCase))
                            {
                                foundSection = true; continue;
                            }
                            if (foundSection) break;
                        }
                        if (foundSection)
                        {
                            if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith(";")) { hasKeys = true; break; }
                        }
                    }
                    if (foundSection && !hasKeys)
                    {
                        lines.RemoveAll(l => string.Equals(l.Trim(), "[" + DOWNLOAD_SECTION + "]", StringComparison.OrdinalIgnoreCase));
                    }

                    File.WriteAllLines(_configPath, lines);
                }
            }
        }
        catch (Exception ex) { LoggerHelper.Warn("Cleanup persisted entry failed: " + ex.Message); }
    }

    public static void StartNow(DownloadTaskModel task
    )
    {
        lock (_lock)
        {
            LoggerHelper.Info($"StartNow called for {task.OfferId}");
            if (_active != null)
            {
                // 暂停当前激活任务并加入队列
                _active.Pause();
                PersistTask(_active);
                _queue.Enqueue(_active);
                _active = null;
            }
            // 从队列移除并启动目标任务
            RemoveFromQueue(task.OfferId);
            _active = task;
            task.Status = DownloadStatus.Waiting;
            _ = RunDownloadAsync(_active);
            ActiveTaskChanged?.Invoke(_active);
        }
    }

    private static void TryStartNext()
    {
        lock (_lock)
        {
            // 如果已有激活任务则返回
            if (_active != null) return;
            // 仅启动未被暂停的任务
            DownloadTaskModel next = null;
            var tempList = new List<DownloadTaskModel>();
            while (_queue.TryDequeue(out var candidate))
            {
                if (next == null && candidate.Status != DownloadStatus.Paused)
                {
                    next = candidate;
                }
                else
                {
                    tempList.Add(candidate);
                }
            }
            // 重新入队剩余任务
            foreach (var t in tempList) _queue.Enqueue(t);

            if (next == null) return;
            _active = next;
            _active.Status = DownloadStatus.Waiting;
            LoggerHelper.Info($"TryStartNext: starting {_active.OfferId}");
            _ = RunDownloadAsync(_active);
            ActiveTaskChanged?.Invoke(_active);
        }
    }

    private static void RemoveFromQueue(string offerId)
    {
        var list = _queue.ToList();
        list.RemoveAll(t => t.OfferId == offerId);
        while (_queue.TryDequeue(out _)) { }
        foreach (var t in list) _queue.Enqueue(t);
    }

    private static async Task RunDownloadAsync(DownloadTaskModel task)
    {
        LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.RunDownloadStart", task.OfferId));
        _running[task.OfferId] = true;
        try
        {
            if (task.TokenSource == null || task.TokenSource.IsCancellationRequested)
            {
                task.TokenSource = new CancellationTokenSource();
            }

            task.Status = DownloadStatus.Downloading;
            ActiveTaskChanged?.Invoke(task); // 立即通知 UI

            if (task.TotalBytes == 0 || string.IsNullOrWhiteSpace(task.JitUrl))
            {
                var result = await Api.EaApi.GetDownloadUrl(task.OfferId);
                if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Content))
                {
                    Fail(task, I18nHelper.I18n._("Helper.DownloadHelper.GetDownloadUrlFailed"));
                    return;
                }
                
                var data = JsonNode.Parse(result.Content);
                var jit = data?["jitUrl"];
                task.JitUrl = jit?["url"]?.ToString();
                var sizeStr = jit?["archiveSize"]?.ToString();
                if (long.TryParse(sizeStr, out var bytes)) task.TotalBytes = bytes;
                PersistTask(task); // 持久化 URL 和大小，便于恢复
            }
            if (string.IsNullOrWhiteSpace(task.JitUrl)) 
            { 
                Fail(task, I18nHelper.I18n._("Helper.DownloadHelper.UrlEmpty")); 
                return; 
            }

            // 启用智能DNS预热
            await WarmupAkamaiDns(task.JitUrl);

            await DownloadSingleAsync(task);

            // 下载完成后检查状态 - 优先检查用户中断
            if (task.RequestPause || task.Status == DownloadStatus.Paused)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.DownloadPaused"));
                task.Status = DownloadStatus.Paused;
                return;
            }
            
            if (task.RequestCancel || task.Status == DownloadStatus.Cancelled)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.DownloadCancelled"));
                task.Status = DownloadStatus.Cancelled;
                return;
            }
            
            if (task.Status == DownloadStatus.Failed)
            {
                return;
            }

            // 设置进度为0，开始安装阶段
            task.Progress = 0;
            task.SpeedBytesPerSec = 0;
            
            task.Status = DownloadStatus.Installing;
            await ExtractAndCleanupAsync(task);
            
            task.Status = DownloadStatus.InstallingRuntime;
            await RunRuntimeInstallAsync(task);
            
            task.Status = DownloadStatus.Completed;
            DeletePersisted(task);
            
            // 游戏下载完成，发送Windows系统通知
            var gameName = task.GameName;
            var installDir = task.InstallDir;
            
            NotifierHelper.SystemSuccessClickable(
                I18nHelper.I18n._("Helper.DownloadHelper.SystemNotificationTitle"),
                I18nHelper.I18n._("Helper.DownloadHelper.SystemNotificationMessage", gameName),
                () =>
                {
                    // 点击通知打开安装目录
                    try
                    {
                        if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", installDir);
                            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.OpenInstallDir", installDir));
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.OpenInstallDirError", ex.Message));
                    }
                });
        }
        catch (Exception ex)
        {
            // 只有非用户中断的情况才记录为失败
            LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.RunDownloadException", task.OfferId, ex.Message));
            if (task.Status != DownloadStatus.Paused && 
                task.Status != DownloadStatus.Cancelled)
            {
                Fail(task, ex.Message);
            }
        }
        finally
        {
            _running[task.OfferId] = false;
            lock (_lock)
            {
                // 如果任务未暂停时，清空激活任务，否则保持暂停状态，以便 UI 展示。
                if (task.Status != DownloadStatus.Paused)
                {
                    _active = null;
                    // notify UI that there is no active task
                    ActiveTaskChanged?.Invoke(null);
                    TryStartNext();
                }
                else
                {
                    // 任务暂停，仍需通知 UI
                    ActiveTaskChanged?.Invoke(_active);
                }
            }
        }
    }

    private static async Task DownloadSingleAsync(DownloadTaskModel task)
    {
        FileHelper.CreateDirectory(task.InstallDir);
        string target = Path.Combine(task.InstallDir, task.OfferId + ".zip");

        long existing = 0;
        bool shouldResume = false;
        
        if (File.Exists(target))
        {
            existing = new FileInfo(target).Length;
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.FoundExistingFile", FormatBytes(existing), FormatBytes(task.TotalBytes)));
            
            if (task.TotalBytes > 0 && existing >= task.TotalBytes)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.FileComplete"));
                task.AddDownloadedBytes(existing - task.DownloadedBytes);
                task.Progress = 100;
                return;
            }
            else if (existing > 0 && existing < task.TotalBytes)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ResumeFrom", FormatBytes(existing)));
                shouldResume = true;
                task.AddDownloadedBytes(existing - task.DownloadedBytes);
            }
            else if (existing > 0)
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Helper.DownloadHelper.FileSizeMismatch", FormatBytes(existing)));
                try { File.Delete(target); } catch { }
                existing = 0;
                task.AddDownloadedBytes(-task.DownloadedBytes);
            }
        }

        // DNS预热，优化Akamai CDN连接
        await WarmupAkamaiDns(task.JitUrl);

        // 检测服务器是否支持Range请求（用于分片下载）
        bool supportsRange = await CheckRangeSupport(task.JitUrl, task.TokenSource?.Token ?? CancellationToken.None);
        
        // 如果文件>100MB且支持Range，使用分片下载
        const long ChunkDownloadThreshold = 100 * 1024 * 1024; // 100MB
        bool useChunkedDownload = task.TotalBytes > ChunkDownloadThreshold && supportsRange;
        
        if (useChunkedDownload)
        {
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.UseChunkedDownload", FormatBytes(task.TotalBytes)));
            await DownloadChunkedAsync(task, target, existing);
        }
        else
        {
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.UseSingleStreamDownload"));
            await DownloadStreamSingleAsync(task, target, existing, shouldResume);
        }
    }
    
    /// <summary>
    /// 单流下载（原有双缓冲优化逻辑）
    /// </summary>
    private static async Task DownloadStreamSingleAsync(DownloadTaskModel task, string target, long existing, bool shouldResume)
    {
        FileStream fs = null;
        Stream stream = null;
        HttpResponseMessage res = null;
        bool cleanExit = false;

        try
        {
            const int FileBufferSize = 8 * 1024 * 1024;
            fs = new FileStream(target, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, FileBufferSize, 
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            
            if (shouldResume && existing > 0)
            {
                fs.Seek(existing, SeekOrigin.Begin);
            }
            else
            {
                fs.SetLength(0);
                existing = 0;
                task.AddDownloadedBytes(-task.DownloadedBytes);
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, task.JitUrl);
            if (shouldResume && existing > 0)
            {
                req.Headers.Range = new RangeHeaderValue(existing, null);
                LoggerHelper.Info($"Requesting range from byte {existing}");
            }

            res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, task.TokenSource?.Token ?? CancellationToken.None);
            LoggerHelper.Info($"HTTP response: {(int)res.StatusCode} {res.ReasonPhrase}");
            
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    LoggerHelper.Warn("Server returned 416 - restarting from beginning");
                    fs.Close();
                    fs.Dispose();
                    fs = null;
                    
                    try
                    {
                        File.Delete(target);
                        LoggerHelper.Info("Deleted existing file");
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Error($"Failed to delete file: {ex.Message}");
                        throw;
                    }
                    
                    existing = 0;
                    task.AddDownloadedBytes(-task.DownloadedBytes);
                    task.ConsecutiveFailures = 0;
                    
                    fs = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, FileBufferSize, 
                        FileOptions.Asynchronous | FileOptions.SequentialScan);
                    
                    res?.Dispose();
                    using var newReq = new HttpRequestMessage(HttpMethod.Get, task.JitUrl);
                    res = await _httpClient.SendAsync(newReq, HttpCompletionOption.ResponseHeadersRead, task.TokenSource?.Token ?? CancellationToken.None);
                    
                    if (!res.IsSuccessStatusCode)
                    {
                        task.ConsecutiveFailures++;
                        if (task.ConsecutiveFailures >= 3) { Fail(task, $"HTTP {(int)res.StatusCode}"); }
                        return;
                    }
                }
                else
                {
                    task.ConsecutiveFailures++;
                    if (task.ConsecutiveFailures >= 3) { Fail(task, $"HTTP {(int)res.StatusCode}"); }
                    return;
                }
            }
            
            task.ConsecutiveFailures = 0;

            if (res.Content.Headers.ContentLength.HasValue)
            {
                var contentLength = res.Content.Headers.ContentLength.Value;
                task.TotalBytes = shouldResume && existing > 0 ? existing + contentLength : contentLength;
                LoggerHelper.Info($"Content length: {FormatBytes(contentLength)}, Total: {FormatBytes(task.TotalBytes)}");
                PersistTask(task);
            }

            stream = await res.Content.ReadAsStreamAsync();

            const int BufferSize = 4 * 1024 * 1024;
            byte[] buffer1 = new byte[BufferSize];
            byte[] buffer2 = new byte[BufferSize];
            byte[] currentReadBuffer = buffer1;
            
            var sw = Stopwatch.StartNew();
            long lastBytes = task.DownloadedBytes;
            var limiter = _limiters.GetOrAdd(task.OfferId, _ => new RateLimiter());
            limiter.SetLimit(_globalLimitBps);
            long lastPersist = Environment.TickCount64;
            long lastUiUpdate = Environment.TickCount64; // 新增：UI更新节流
            
            Task<int> readTask = stream.ReadAsync(currentReadBuffer, 0, currentReadBuffer.Length, task.TokenSource?.Token ?? CancellationToken.None);
            Task writeTask = Task.CompletedTask;

            while (true)
            {
                int read = await readTask;
                if (read == 0) break;

                var nextReadBuffer = (currentReadBuffer == buffer1) ? buffer2 : buffer1;
                readTask = stream.ReadAsync(nextReadBuffer, 0, nextReadBuffer.Length, task.TokenSource?.Token ?? CancellationToken.None);

                await writeTask;

                if (task.RequestPause || task.RequestCancel)
                {
                    await fs.FlushAsync();
                    task.Status = task.RequestPause ? DownloadStatus.Paused : DownloadStatus.Cancelled;
                    cleanExit = true;
                    return;
                }

                writeTask = fs.WriteAsync(currentReadBuffer, 0, read, task.TokenSource?.Token ?? CancellationToken.None);
                task.AddDownloadedBytes(read);

                try
                {
                    await limiter.WaitAsync(read, task.TokenSource?.Token ?? CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    await writeTask;
                    await fs.FlushAsync();
                    task.Status = task.RequestPause ? DownloadStatus.Paused : DownloadStatus.Cancelled;
                    cleanExit = true;
                    return;
                }

                // UI更新节流：每200ms更新一次（而非每次读取）
                var currentTick = Environment.TickCount64;
                if (currentTick - lastUiUpdate >= 200)
                {
                    task.Progress = task.TotalBytes > 0 ? (double)task.DownloadedBytes / task.TotalBytes * 100.0 : 0;
                    
                    if (sw.ElapsedMilliseconds >= 1000)
                    {
                        var delta = task.DownloadedBytes - lastBytes;
                        var inst = delta / (sw.ElapsedMilliseconds / 1000.0);
                        task.SpeedBytesPerSec = MovingAverage(task.SpeedBytesPerSec, inst);
                        lastBytes = task.DownloadedBytes;
                        sw.Restart();
                    }
                    
                    lastUiUpdate = currentTick;
                }

                if (Environment.TickCount64 - lastPersist > 30000)
                {
                    await writeTask;
                    PersistTask(task);
                    lastPersist = Environment.TickCount64;
                }

                currentReadBuffer = nextReadBuffer;
            }

            await writeTask;
            await fs.FlushAsync();
            PersistTask(task);
            
            if (!task.RequestPause && !task.RequestCancel)
            {
                task.Progress = 100;
                cleanExit = true;
            }
        }
        catch (OperationCanceledException)
        {
            if (fs != null) { try { await fs.FlushAsync(); } catch { } }
            task.Status = task.RequestPause ? DownloadStatus.Paused : DownloadStatus.Cancelled;
            cleanExit = true;
        }
        catch (Exception ex)
        {
            if (fs != null) { try { await fs.FlushAsync(); } catch { } }
            if (!task.RequestPause && !task.RequestCancel)
            {
                LoggerHelper.Error($"Download error: {ex.Message}");
                Fail(task, ex.Message);
            }
        }
        finally
        {
            if (stream != null) { try { await stream.DisposeAsync(); } catch { } }
            if (res != null) { try { res.Dispose(); } catch { } }
            if (fs != null)
            {
                try
                {
                    if (!cleanExit) { try { await fs.FlushAsync(); } catch { } }
                    fs.Close();
                    fs.Dispose();
                }
                catch { }
            }

            task.ResetStoppingFlag();
            if (task.Status == DownloadStatus.Paused || task.Status == DownloadStatus.Cancelled)
            {
                await Task.Delay(200);
            }

            LoggerHelper.Info($"DownloadStreamSingleAsync finished, status {task.Status}");
        }
    }

    /// <summary>
    /// 检查服务器是否支持Range请求
    /// </summary>
    private static async Task<bool> CheckRangeSupport(string url, CancellationToken token)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Head, url);
            var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);
            
            // 检查Accept-Ranges头
            if (res.Headers.TryGetValues("Accept-Ranges", out var values))
            {
                var acceptRanges = values.FirstOrDefault();
                bool supports = !string.IsNullOrEmpty(acceptRanges) && 
                               !acceptRanges.Equals("none", StringComparison.OrdinalIgnoreCase);
                LoggerHelper.Info($"Server Accept-Ranges: {acceptRanges}, Supports: {supports}");
                return supports;
            }
            
            LoggerHelper.Info("Server does not advertise Range support");
            return false;
        }
        catch (Exception ex)
        {
            LoggerHelper.Warn($"Failed to check Range support: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 分片并发下载，适用于大文件
    /// </summary>
    private static async Task DownloadChunkedAsync(DownloadTaskModel task, string target, long startFrom)
    {
        const long ChunkSize = 50 * 1024 * 1024; // 每片50MB
        const int MaxConcurrentChunks = 4; // 最多4个并发下载
        const int FileBufferSize = 8 * 1024 * 1024;
        
        FileStream fs = null;
        bool cleanExit = false;
        var cancellationSource = new CancellationTokenSource();
        
        try
        {
            // 打开文件（允许随机读写）
            fs = new FileStream(target, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 
                FileBufferSize, FileOptions.Asynchronous | FileOptions.RandomAccess);
            
            if (startFrom > 0)
            {
                fs.SetLength(Math.Max(fs.Length, startFrom));
            }

            long totalBytes = task.TotalBytes;
            long currentPosition = startFrom;
            
            var sw = Stopwatch.StartNew();
            long lastBytes = task.DownloadedBytes;
            var limiter = _limiters.GetOrAdd(task.OfferId, _ => new RateLimiter());
            limiter.SetLimit(_globalLimitBps);
            long lastPersist = Environment.TickCount64;
            long lastUiUpdate = Environment.TickCount64;
            
            // 分片下载队列
            var chunkQueue = new ConcurrentQueue<(long start, long end)>();
            
            // 计算所有分片
            long pos = currentPosition;
            while (pos < totalBytes)
            {
                long chunkEnd = Math.Min(pos + ChunkSize - 1, totalBytes - 1);
                chunkQueue.Enqueue((pos, chunkEnd));
                pos = chunkEnd + 1;
            }
            
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ChunkInfo", 
                chunkQueue.Count, FormatBytes(ChunkSize), MaxConcurrentChunks));
            
            // 下载进度统计
            var downloadedChunks = 0;
            var totalChunks = chunkQueue.Count;
            var chunkLock = new object();
            var hasError = false;
            
            // 启动并发下载线程
            var downloadTasks = Enumerable.Range(0, MaxConcurrentChunks)
                .Select(async threadId =>
                {
                    while (chunkQueue.TryDequeue(out var chunk))
                    {
                        // 优先检查暂停/取消请求
                        if (task.RequestPause || task.RequestCancel || cancellationSource.IsCancellationRequested)
                        {
                            return;
                        }
                        
                        // 检查是否其他线程失败
                        if (hasError)
                        {
                            return;
                        }
                        
                        var (start, end) = chunk;
                        int retries = 0;
                        const int MaxRetries = 3;
                        
                        while (retries < MaxRetries)
                        {
                            try
                            {
                                // 每次重试前再次检查暂停/取消
                                if (task.RequestPause || task.RequestCancel || cancellationSource.IsCancellationRequested)
                                {
                                    return;
                                }
                                
                                await DownloadChunkAsync(task, fs, start, end, limiter, chunkLock, cancellationSource.Token);
                                
                                lock (chunkLock)
                                {
                                    downloadedChunks++;
                                    
                                    // UI更新频率：每200ms更新一次，避免UI卡顿
                                    var currentTick = Environment.TickCount64;
                                    if (currentTick - lastUiUpdate >= 200)
                                    {
                                        // 更新进度
                                        task.Progress = task.TotalBytes > 0 
                                            ? (double)task.DownloadedBytes / task.TotalBytes * 100.0 
                                            : 0;
                                        
                                        // 计算速度
                                        if (sw.ElapsedMilliseconds >= 1000)
                                        {
                                            var delta = task.DownloadedBytes - lastBytes;
                                            var inst = delta / (sw.ElapsedMilliseconds / 1000.0);
                                            task.SpeedBytesPerSec = MovingAverage(task.SpeedBytesPerSec, inst);
                                            lastBytes = task.DownloadedBytes;
                                            sw.Restart();
                                        }
                                        
                                        lastUiUpdate = currentTick;
                                    }
                                }
                
                                break; // 成功，跳出重试循环
                            }
                            catch (OperationCanceledException)
                            {
                                // 取消操作是正常的，不重试
                                return;
                            }
                            catch (Exception ex)
                            {
                                retries++;
                                
                                // 检查当前是否请求停止
                                if (task.RequestPause || task.RequestCancel || cancellationSource.IsCancellationRequested)
                                {
                                    return;
                                }
                                
                                LoggerHelper.Warn(I18nHelper.I18n._("Helper.DownloadHelper.ChunkDownloadRetry", 
                                    threadId, retries, MaxRetries, ex.Message));
                                
                                if (retries >= MaxRetries)
                                {
                                    LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.ChunkDownloadFailed", 
                                        threadId, FormatBytes(start), FormatBytes(end), MaxRetries));
                                    hasError = true;
                                    cancellationSource.Cancel(); // 通知其他线程停止
                                    throw;
                                }
                                
                                // 指数退避
                                await Task.Delay(1000 * retries, task.TokenSource?.Token ?? CancellationToken.None);
                            }
                        }
                        
                        // 用于持久化
                        if (Environment.TickCount64 - lastPersist > 30000)
                        {
                            lock (chunkLock)
                            {
                                PersistTask(task);
                                lastPersist = Environment.TickCount64;
                            }
                        }
                    }
                })
                .ToArray();
            
            // 等待所有下载完成
            try
            {
                await Task.WhenAll(downloadTasks);
            }
            catch (Exception ex)
            {
                // 如果是因为暂停/取消，不记录为错误
                if (task.RequestPause || task.RequestCancel)
                {
                    LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ChunkedInterrupted"));
                }
                else
                {
                    LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.ChunkedError", ex.Message));
                    throw;
                }
            }
            
            // 确保所有数据写入磁盘
            await fs.FlushAsync();
            
            // 检查是否中断
            if (task.RequestPause)
            {
                task.Status = DownloadStatus.Paused;
                cleanExit = true;
                PersistTask(task); // 持久化暂停状态
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ChunkedDownloadPaused", FormatBytes(task.DownloadedBytes)));
                return;
            }
            
            if (task.RequestCancel)
            {
                task.Status = DownloadStatus.Cancelled;
                cleanExit = true;
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ChunkedDownloadCancelled"));
                return;
            }
            
            // 检查是否有错误
            if (hasError)
            {
                throw new Exception(I18nHelper.I18n._("Helper.DownloadHelper.ChunksFailed"));
            }
            
            // 完成
            PersistTask(task);
            task.Progress = 100;
            cleanExit = true;
            
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ChunkedDownloadCompleted", FormatBytes(task.DownloadedBytes)));
        }
        catch (Exception ex)
        {
            if (fs != null)
            {
                try { await fs.FlushAsync(); } catch { }
            }
            
            // 只有非用户中断的错误才标记为失败
            if (!task.RequestPause && !task.RequestCancel)
            {
                LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.ChunkedError", ex.Message));
                Fail(task, ex.Message);
            }
        }
        finally
        {
            cancellationSource?.Dispose();
            
            if (fs != null)
            {
                try
                {
                    if (!cleanExit)
                    {
                        try { await fs.FlushAsync(); } catch { }
                    }
                    fs.Close();
                    fs.Dispose();
                }
                catch { }
            }
            
            task.ResetStoppingFlag();
            
            if (task.Status == DownloadStatus.Paused || task.Status == DownloadStatus.Cancelled)
            {
                await Task.Delay(200);
            }
        }
    }

    /// <summary>
    /// 下载单个分片
    /// </summary>
    private static async Task DownloadChunkAsync(DownloadTaskModel task, FileStream fs, long start, long end, 
        RateLimiter limiter, object fsLock, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, 
            task.TokenSource?.Token ?? CancellationToken.None);
        
        using var req = new HttpRequestMessage(HttpMethod.Get, task.JitUrl);
        req.Headers.Range = new RangeHeaderValue(start, end);
        
        using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
        
        if (!res.IsSuccessStatusCode && res.StatusCode != System.Net.HttpStatusCode.PartialContent)
        {
            throw new Exception($"HTTP {(int)res.StatusCode}: {res.ReasonPhrase}");
        }
        
        using var stream = await res.Content.ReadAsStreamAsync();
        
        const int BufferSize = 256 * 1024; // 256KB per chunk read
        byte[] buffer = new byte[BufferSize];
        long position = start;
        
        int read;
        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token)) > 0)
        {
            // 频繁检查取消请求
            if (task.RequestPause || task.RequestCancel || cancellationToken.IsCancellationRequested)
            {
                linkedCts.Token.ThrowIfCancellationRequested();
            }
            
            // 限速
            await limiter.WaitAsync(read, linkedCts.Token);
            
            // 写入文件（加锁保证线程安全）
            lock (fsLock)
            {
                fs.Seek(position, SeekOrigin.Begin);
                fs.Write(buffer, 0, read);
                position += read;
            }
            
            // 更新已下载字节数
            task.AddDownloadedBytes(read);
        }
    }

    private static void PersistTask(DownloadTaskModel task)
    {
        try
        {
            FileHelper.CreateFile(_configPath);
            string value = string.Join('|', new[]
            {
                task.InstallDir ?? string.Empty,
                task.InstallLanguage ?? string.Empty,
                task.TotalBytes.ToString(),
                task.DownloadedBytes.ToString(),
                task.JitUrl ?? string.Empty
            });
            IniHelper.WriteString(DOWNLOAD_SECTION, task.OfferId, value, _configPath);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("PersistTask error: " + ex.Message);
        }
    }

    private static void DeletePersisted(DownloadTaskModel task)
    {
        try { IniHelper.DeleteKey(DOWNLOAD_SECTION, task.OfferId, _configPath); }
        catch (Exception ex) { LoggerHelper.Error("DeletePersisted error: " + ex.Message); }
    }

    private static IEnumerable<DownloadTaskModel> ReadAllTasks()
    {
        var list = new List<DownloadTaskModel>();
        try
        {
            if (!File.Exists(_configPath)) return list;
            var lines = File.ReadAllLines(_configPath);
            bool inSection = false;
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("[") && line.Trim().EndsWith("]"))
                { inSection = string.Equals(line.Trim(), "[" + DOWNLOAD_SECTION + "]", StringComparison.OrdinalIgnoreCase); continue; }
                if (!inSection || string.IsNullOrWhiteSpace(line)) continue;
                var kv = line.Split('=', 2); if (kv.Length != 2) continue;
                var offerId = kv[0].Trim(); var val = kv[1].Trim(); if (string.IsNullOrWhiteSpace(val)) continue;
                var parts = val.Split('|');
                var model = new DownloadTaskModel { OfferId = offerId, InstallDir = parts.ElementAtOrDefault(0) ?? string.Empty, InstallLanguage = parts.ElementAtOrDefault(1) ?? string.Empty, GameName = offerId };
                if (long.TryParse(parts.ElementAtOrDefault(2), out var total)) model.TotalBytes = total;
                if (long.TryParse(parts.ElementAtOrDefault(3), out var downloaded)) model.AddDownloadedBytes(downloaded);
                model.JitUrl = parts.ElementAtOrDefault(4) ?? string.Empty;
                list.Add(model);
            }
        }
        catch (Exception ex) { LoggerHelper.Error("ReadAllTasks error: " + ex.Message); }
        return list;
    }

    private static void Fail(DownloadTaskModel task, string reason)
    {
        task.Status = DownloadStatus.Failed;
        LoggerHelper.Error($"Download failed ({task.OfferId}): {reason}");
        // 通知 UI 任务状态变更
        try { ActiveTaskChanged?.Invoke(task); } catch { }
    }

    private const double DownloadPhaseWeight = 0.90; // 0-90% 下载，90-100% 安装

    private static async Task ExtractAndCleanupAsync(DownloadTaskModel task)
    {
        string zipPath = Path.Combine(task.InstallDir, task.OfferId + ".zip");
        if (!File.Exists(zipPath))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Helper.DownloadHelper.ZipNotFound", zipPath));
            return;
        }

        var fileInfo = new FileInfo(zipPath);
        LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ExtractStart", zipPath, FormatBytes(fileInfo.Length)));

        bool success = false;
        int processedCount = 0;
        int errorCount = 0;
        long extracted = 0;

        try
        {
            // 在后台线程执行解压，避免阻塞UI线程
            await Task.Run(async () =>
            {
                // 使用 SharpCompress 直接遍历压缩包
                using var archive = SharpCompress.Archives.Zip.ZipArchive.Open(zipPath);
                
                var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ExtractArchiveEntries", entries.Count));

                // 计算总大小
                long totalUncompressed = 0;
                foreach (var entry in entries)
                {
                    if (entry.Size > 0 && entry.Size < 1099511627776L)
                    {
                        totalUncompressed += entry.Size;
                    }
                }
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ExtractTotalSize", FormatBytes(totalUncompressed)));

                var sw = Stopwatch.StartNew();
                long last = 0;

                // 检测根目录
                string? root = GetCommonRootFolderSC(entries);
                if (!string.IsNullOrEmpty(root))
                {
                    LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ExtractCommonRoot", root));
                }

                // 直接遍历压缩包进行解压
                const int BufferSize = 4 * 1024 * 1024; // 增大缓冲（加快解压）
                long lastUiUpdate = 0;

                foreach (var entry in entries)
                {
                    if (task.RequestPause || task.RequestCancel)
                    {
                        LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ExtractInterrupted", task.RequestPause, task.RequestCancel));
                        return;
                    }

                    var rel = entry.Key;
                    
                    // 移除根目录
                    if (!string.IsNullOrEmpty(root) && rel.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    {
                        rel = rel.Substring(root.Length);
                    }

                    // 标准化路径分隔符
                    rel = rel.Replace('/', Path.DirectorySeparatorChar);
                    
                    string outPath = Path.Combine(task.InstallDir, rel);
                    string? directory = Path.GetDirectoryName(outPath);

                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    try
                    {
                        // 使用 SharpCompress 的解压器
                        using var entryStream = entry.OpenEntryStream();
                        using var outS = new FileStream(
                            outPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            BufferSize,
                            FileOptions.Asynchronous | FileOptions.SequentialScan); // 顺序优化标志

                        byte[] buf = new byte[BufferSize];
                        int read;

                        while ((read = await entryStream.ReadAsync(buf, 0, buf.Length, task.TokenSource?.Token ?? CancellationToken.None)) > 0)
                        {
                            await outS.WriteAsync(buf, 0, read, task.TokenSource?.Token ?? CancellationToken.None);
                            extracted += read;

                            // UI更新频率：每500ms更新一次（解压时更新可缓）
                            if (sw.ElapsedMilliseconds - lastUiUpdate >= 500)
                            {
                                // Update progress
                                if (totalUncompressed > 0)
                                {
                                    var ratio = (double)extracted / totalUncompressed;
                                    // 0-90% 对应解压阶段
                                    task.Progress = ratio * 90.0;
                                }

                                // Update speed calculation
                                if (sw.ElapsedMilliseconds >= 1000)
                                {
                                    var delta = extracted - last;
                                    var inst = delta / (sw.ElapsedMilliseconds / 1000.0);
                                    task.SpeedBytesPerSec = MovingAverage(task.SpeedBytesPerSec, inst);
                                    last = extracted;
                                    sw.Restart();
                                }
                                
                                lastUiUpdate = sw.ElapsedMilliseconds;
                            }
                        }

                        // 设置文件时间戳
                        try
                        {
                            if (entry.LastModifiedTime.HasValue)
                            {
                                File.SetLastWriteTime(outPath, entry.LastModifiedTime.Value);
                            }
                        }
                        catch { /* Ignore timestamp errors */ }

                        processedCount++;
                        if (processedCount % 100 == 0)
                        {
                            LoggerHelper.Debug(I18nHelper.I18n._("Helper.DownloadHelper.ExtractProgress", 
                                processedCount, entries.Count, FormatBytes(extracted)));
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.ExtractFileFailed", entry.Key, ex.Message));

                        if (errorCount >= 20)
                        {
                            LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.ExtractTooManyErrors", errorCount));
                            throw;
                        }
                    }
                }

                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.ExtractSuccess", 
                    processedCount, FormatBytes(extracted), errorCount));

                if (errorCount > 0)
                {
                    LoggerHelper.Warn(I18nHelper.I18n._("Helper.DownloadHelper.ExtractWithErrors", errorCount));
                    success = true;
                    task.Progress = 90.0;
                }
                else
                {
                    task.Progress = 90.0;
                    success = true;
                }
            }, task.TokenSource?.Token ?? CancellationToken.None); // Task.Run结束
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.ExtractFailed", ex.Message));
            LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.ExtractExceptionType", ex.GetType().Name));
            if (ex.InnerException != null)
            {
                LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.ExtractInnerException", ex.InnerException.Message));
            }
            throw;
        }
        finally
        {
            if (success)
            {
                try
                {
                    LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.DeleteZip", zipPath));
                    File.Delete(zipPath);
                    LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.DeleteZipSuccess"));
                }
                catch (Exception ex)
                {
                    LoggerHelper.Warn(I18nHelper.I18n._("Helper.DownloadHelper.DeleteZipFailed", ex.Message));
                }
            }
            else
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Helper.DownloadHelper.ZipRetained"));
            }
        }
    }

    /// <summary>
    /// 使用 SharpCompress 的方式检测根目录
    /// </summary>
    private static string? GetCommonRootFolderSC(IEnumerable<IArchiveEntry> entries)
    {
        var names = entries.Where(e => !e.IsDirectory && !string.IsNullOrEmpty(e.Key))
                          .Select(e => e.Key)
                          .ToList();
        
        if (names.Count == 0) return null;

        string first = names[0];
        int slash = first.IndexOf('/') >= 0 ? first.IndexOf('/') : first.IndexOf('\\');
        if (slash <= 0) return null;

        string root = first.Substring(0, slash + 1);
        foreach (var n in names)
        {
            if (!n.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;
        }

        return root;
    }

    /// <summary>
    /// 递归查找 touchup.exe
    /// </summary>
    private static string FindTouchupExe(string rootDir, int currentDepth = 0, int maxDepth = 5)
    {
        if (!Directory.Exists(rootDir) || currentDepth >= maxDepth)
            return null;

        try
        {
            // 在当前目录查找 touchup.exe
            var touchupPath = Path.Combine(rootDir, "touchup.exe");
            if (File.Exists(touchupPath))
                return touchupPath;
            
            // 递归搜索子目录
            var subDirs = Directory.GetDirectories(rootDir);
            foreach (var subDir in subDirs)
            {
                var found = FindTouchupExe(subDir, currentDepth + 1, maxDepth);
                if (!string.IsNullOrEmpty(found))
                    return found;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Warn($"Error searching for touchup.exe in {rootDir}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 递归查找 installerdata.xml
    /// </summary>
    private static string FindInstallerDataXml(string rootDir, int currentDepth = 0, int maxDepth = 5)
    {
        if (!Directory.Exists(rootDir) || currentDepth >= maxDepth)
            return null;

        try
        {
            // 首先在当前目录的 __Installer 中查找
            string xmlPath = Path.Combine(rootDir, "__Installer", "installerdata.xml");
            if (File.Exists(xmlPath))
                return xmlPath;
            
            // 递归查找所有 __Installer 目录
            var installerDirs = Directory.GetDirectories(rootDir, "__Installer", SearchOption.TopDirectoryOnly);
            foreach (var installerDir in installerDirs)
            {
                xmlPath = Path.Combine(installerDir, "installerdata.xml");
                if (File.Exists(xmlPath))
                    return xmlPath;
                
                // 递归深入
                var found = FindInstallerDataXml(installerDir, currentDepth + 1, maxDepth);
                if (!string.IsNullOrEmpty(found))
                    return found;
            }
            
            // 查找其他子目录
            var subDirs = Directory.GetDirectories(rootDir);
            foreach (var subDir in subDirs)
            {
                if (Path.GetFileName(subDir) == "__Installer")
                    continue;
                
                var found = FindInstallerDataXml(subDir, currentDepth + 1, maxDepth);
                if (!string.IsNullOrEmpty(found))
                    return found;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Warn($"Error searching for installerdata.xml in {rootDir}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 从 installerdata.xml 读取 touchup 启动参数
    /// </summary>
    private static string GetTouchupParametersFromXml(string installDir, string locale)
    {
        try
        {
            // 查找 installerdata.xml
            string xmlPath = Path.Combine(installDir, "__Installer", "installerdata.xml");
            
            if (!File.Exists(xmlPath))
            {
                // 尝试递归查找
                xmlPath = FindInstallerDataXml(installDir);
            }
            
            if (string.IsNullOrEmpty(xmlPath) || !File.Exists(xmlPath))
            {
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.TouchupParametersNotFound"));
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.UsingDefaultTouchupParameters"));
                return $"install -locale {locale} -installPath \"{installDir}\" -autologging";
            }
            
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.FoundInstallerDataXml", xmlPath));
            
            // 解析 XML
            var xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.Load(xmlPath);
            
            // 查找 touchup/parameters 节点
            var parametersNode = xmlDoc.SelectSingleNode("//DiPManifest/touchup/parameters");
            
            if (parametersNode != null && !string.IsNullOrWhiteSpace(parametersNode.InnerText))
            {
                string parameters = parametersNode.InnerText.Trim();
                
                // 替换占位符
                parameters = parameters.Replace("{locale}", locale);
                parameters = parameters.Replace("{installLocation}", installDir);
                
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.UsingCustomTouchupParameters"));
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.TouchupParametersFound", parameters));
                
                return parameters;
            }
            else
            {
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.TouchupParametersNotFound"));
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.UsingDefaultTouchupParameters"));
                return $"install -locale {locale} -installPath \"{installDir}\" -autologging";
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Helper.DownloadHelper.ParseInstallerDataXmlFailed", ex.Message));
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.UsingDefaultTouchupParameters"));
            return $"install -locale {locale} -installPath \"{installDir}\" -autologging";
        }
    }

    private static async Task RunRuntimeInstallAsync(DownloadTaskModel task)
    {
        try
        {
            string installerDir = Path.Combine(task.InstallDir, "__Installer");
            
            // 递归查找touchup.exe
            string exePath = FindTouchupExe(installerDir);
            
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.FoundTouchupExe", exePath));
                
                // 尝试从 installerdata.xml 读取自定义参数
                string touchupArgs = GetTouchupParametersFromXml(task.InstallDir, task.InstallLanguage);
                
                // 90-99% 进度（进度条，仅视觉）
                var progressTask = Task.Run(async () =>
                {
                    const double startProgress = 90.0;
                    const double endProgress = 99.0;
                    const int durationSeconds = 15;
                    const int steps = 150;
                    
                    for (int i = 0; i < steps; i++)
                    {
                        if (task.Status != DownloadStatus.InstallingRuntime)
                            break;
                            
                        var progress = startProgress + (endProgress - startProgress) * i / steps;
                        task.Progress = progress;
                        
                        await Task.Delay(durationSeconds * 1000 / steps);
                    }
                });
                
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = touchupArgs,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var p = Process.Start(psi);
                await p.WaitForExitAsync();
                
                // 等待进度动画完成或取消
                try
                {
                    await Task.WhenAny(progressTask, Task.Delay(100));
                }
                catch { }
                
                // 安装完成，设置到100%
                task.Progress = 100.0;
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.RuntimeInstallComplete"));
            }
            else
            {
                LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.NoTouchupExe", installerDir));
                task.Progress = 100.0;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Helper.DownloadHelper.RuntimeInstallFailed", ex.Message));
            task.Progress = 100.0; // 即使失败也设置为100%
        }
    }

    private static readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private class RateLimiter
    {
        private readonly object _sync = new();
        private double _bytesPerSec;
        private double _tokens;
        private Stopwatch _sw = Stopwatch.StartNew();
        public void SetLimit(double bytesPerSec) { lock (_sync) { _bytesPerSec = bytesPerSec; _tokens = bytesPerSec; _sw.Restart(); } }
        public async Task WaitAsync(int bytes, CancellationToken token)
        {
            // 如果未设置限速，立即返回
            if (_bytesPerSec <= 0) return;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                double toWaitMs;
                lock (_sync)
                {
                    // 动态检查，防止运行时被取消限速导致除零或超大等待
                    if (_bytesPerSec <= 0) return;

                    var elapsed = _sw.Elapsed.TotalSeconds;
                    if (elapsed > 0)
                    {
                        _tokens = Math.Min(_bytesPerSec, _tokens + elapsed * _bytesPerSec);
                        _sw.Restart();
                    }
                    if (_tokens >= bytes)
                    {
                        _tokens -= bytes;
                        return;
                    }

                    var deficit = bytes - _tokens;
                    if (_bytesPerSec <= 0) return; // double check

                    // 计算等待毫秒数并进行界限保护，避免生成过大的 TimeSpan
                    toWaitMs = (deficit / _bytesPerSec) * 1000.0;
                    if (double.IsInfinity(toWaitMs) || double.IsNaN(toWaitMs) || toWaitMs < 0)
                        toWaitMs = 1;
                    // 限制最大等待 10 秒，回到循环重新计算令牌
                    const double MaxWaitMs = 10_000;
                    if (toWaitMs > MaxWaitMs) toWaitMs = MaxWaitMs;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1, toWaitMs)), token);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // 退回短等待，避免 TimeSpan 溢出
                    await Task.Delay(100, token);
                }
            }
        }
    }

    private static double MovingAverage(double prev, double sample, double alpha = 0.2) => prev <= 0 ? sample : alpha * sample + (1 - alpha) * prev;

    /// <summary>
    /// 优化Akamai CDN连接：DNS预热
    /// </summary>
    private static async Task WarmupAkamaiDns(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host;
            
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.DnsWarmupStart", host));
            
            // 执行3次DNS查询（Akamai的最佳实践）
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var addresses = await System.Net.Dns.GetHostAddressesAsync(host);
                    if (addresses.Length > 0)
                    {
                        LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.DnsWarmupResult", 
                            i + 1, host, string.Join(", ", addresses.Take(3).Select(a => a.ToString()))));
                    }
                    
                    // 短暂延迟，让DNS系统稳定
                    if (i < 2) await Task.Delay(200);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Warn(I18nHelper.I18n._("Helper.DownloadHelper.DnsWarmupAttemptFailed", i + 1, ex.Message));
                }
            }
            
            LoggerHelper.Info(I18nHelper.I18n._("Helper.DownloadHelper.DnsWarmupComplete", host));
        }
        catch (Exception ex)
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Helper.DownloadHelper.DnsWarmupFailed", ex.Message));
        }
    }
}
