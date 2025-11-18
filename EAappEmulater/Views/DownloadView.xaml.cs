using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Enums;
using EAappEmulater.Helper;
using EAappEmulater.Models;

namespace EAappEmulater.Views;

public partial class DownloadView : UserControl, INotifyPropertyChanged
{
    public ObservableCollection<DownloadItemModel> ActiveDownloads { get; } = new();
    public ObservableCollection<DownloadItemModel> QueuedDownloads { get; } = new();
    public ObservableCollection<DownloadItemModel> CompletedDownloads { get; } = new(); // 新增：已结束列表

    public IRelayCommand<DownloadItemModel> PauseResumeCommand { get; }
    public IRelayCommand<DownloadItemModel> CancelCommand { get; }
    public IRelayCommand<DownloadItemModel> RetryCommand { get; } // 新增：重新下载命令
    public IRelayCommand<DownloadItemModel> OpenFolderCommand { get; } // 新增：打开目录命令

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private DownloadTaskModel _activeTaskSubscribed;
    private int _lastQueueCount = 0; // 记录上次队列数量
    private HashSet<string> _lastQueueIds = new(); // 记录上次队列的OfferId

    public DownloadView()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += DownloadView_Loaded;

        PauseResumeCommand = new RelayCommand<DownloadItemModel>(OnPauseResume);
        CancelCommand = new RelayCommand<DownloadItemModel>(OnCancel);
        RetryCommand = new RelayCommand<DownloadItemModel>(OnRetry);
        OpenFolderCommand = new RelayCommand<DownloadItemModel>(OnOpenFolder);

        DownloadHelper.ActiveTaskChanged += OnActiveTaskChanged;
        DownloadHelper.ResumePersistedPausedTasks();
        foreach (var t in DownloadHelper.GetQueue()) QueuedDownloads.Add(ToItem(t));

        _timer.Tick += (_, __) => RefreshActiveSnapshot();
        _timer.Start();

        CheckBox_GlobalLimit.Checked += (_, __) => ApplyLimit();
        CheckBox_GlobalLimit.Unchecked += (_, __) => ApplyLimit();
        TextBox_GlobalLimit.TextChanged += (_, __) => { if (CheckBox_GlobalLimit.IsChecked == true) ApplyLimit(); };
    }

    private void DownloadView_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= DownloadView_Loaded;
        SyncActive();
    }

    private void OnActiveTaskChanged(DownloadTaskModel task)
    {
        Dispatcher.Invoke(() =>
        {
            // 如果任务是null或已结束状态，触发完整刷新
            if (task == null || 
                task.Status == DownloadStatus.Completed || 
                task.Status == DownloadStatus.Cancelled || 
                task.Status == DownloadStatus.Failed)
            {
                // 先处理旧的激活任务（如果有的话）
                var oldActive = _activeTaskSubscribed;
                if (oldActive != null && 
                    (oldActive.Status == DownloadStatus.Completed || 
                     oldActive.Status == DownloadStatus.Cancelled || 
                     oldActive.Status == DownloadStatus.Failed))
                {
                    // 移动到已结束列表
                    var completedItem = ToItem(oldActive);
                    completedItem.ShowRetryButton = oldActive.Status == DownloadStatus.Cancelled || oldActive.Status == DownloadStatus.Failed;
                    completedItem.ShowOpenFolderButton = oldActive.Status == DownloadStatus.Completed;
                    completedItem.InstallPath = oldActive.InstallDir;
                    
                    var existing = CompletedDownloads.FirstOrDefault(x => x.OfferId == completedItem.OfferId);
                    if (existing == null)
                    {
                        CompletedDownloads.Insert(0, completedItem);
                        LoggerHelper.Info(I18nHelper.I18n._("Views.DownloadView.MovedToCompleted", completedItem.OfferId, oldActive.Status));
                    }
                }
            }
            
            SubscribeToActiveTask(task);
            SyncActive();
        });
    }

    private void SubscribeToActiveTask(DownloadTaskModel task)
    {
        if (_activeTaskSubscribed != null)
            _activeTaskSubscribed.PropertyChanged -= ActiveTask_PropertyChanged;
        _activeTaskSubscribed = task;
        if (_activeTaskSubscribed != null)
            _activeTaskSubscribed.PropertyChanged += ActiveTask_PropertyChanged;
    }

    private void ActiveTask_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(RefreshActiveSnapshot);
    }

    private void OnPauseResume(DownloadItemModel item)
    {
        var active = DownloadHelper.GetActive();
        
        // 检查是否是激活任务
        if (active != null && active.OfferId == item.OfferId)
        {
            // 这是激活的任务
            if (active.Status == DownloadStatus.Downloading || 
                active.Status == DownloadStatus.Installing ||
                active.Status == DownloadStatus.InstallingRuntime)
            {
                // 正在下载/安装，暂停
                DownloadHelper.Pause(active);
            }
            else if (active.Status == DownloadStatus.Paused || 
                     active.Status == DownloadStatus.Waiting)
            {
                // 已暂停/等待，恢复
                DownloadHelper.Resume(active);
            }
        }
        else
        {
            // 这是队列中的任务 - 立即开始下载
            var task = DownloadHelper.GetQueue().FirstOrDefault(t => t.OfferId == item.OfferId);
            if (task != null)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Views.DownloadView.StartNowQueued", task.OfferId));
                DownloadHelper.StartNow(task);
            }
        }
    }

    private void OnCancel(DownloadItemModel item)
    {
        var active = DownloadHelper.GetActive();
        if (active != null && active.OfferId == item.OfferId)
        {
            DownloadHelper.Cancel(active);
        }
        else
        {
            var task = DownloadHelper.GetQueue().FirstOrDefault(t => t.OfferId == item.OfferId);
            if (task != null) DownloadHelper.Cancel(task);
        }
    }

    private void OnRetry(DownloadItemModel item)
    {
        LoggerHelper.Info(I18nHelper.I18n._("Views.DownloadView.RetryDownload", item.OfferId));
        
        // 从已结束列表移除
        CompletedDownloads.Remove(item);
        
        // 创建新的下载任务
        var newTask = new DownloadTaskModel
        {
            OfferId = item.OfferId,
            GameName = item.GameName,
            InstallDir = item.InstallPath ?? string.Empty,
            InstallLanguage = "en_US", // 默认语言
            Status = DownloadStatus.Waiting
        };
        
        DownloadHelper.Enqueue(newTask, autoStart: true);
    }

    private void OnOpenFolder(DownloadItemModel item)
    {
        try
        {
            // 打开安装目录
            var installPath = item.InstallPath;
            if (!string.IsNullOrEmpty(installPath) && System.IO.Directory.Exists(installPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", installPath);
                LoggerHelper.Info(I18nHelper.I18n._("Views.DownloadView.OpenedFolder", installPath));
            }
            else
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Views.DownloadView.FolderNotFound", installPath));
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Views.DownloadView.OpenFolderFailed", ex.Message));
        }
    }

    private void RefreshActiveSnapshot()
    {
        var active = DownloadHelper.GetActive();
        if (active == null)
        {
            ActiveDownloads.Clear();
            
            // 只在队列变化时更新
            RefreshQueueIfChanged();
            return;
        }
        
        // 检查激活任务是否已结束
        if (active.Status == DownloadStatus.Completed || active.Status == DownloadStatus.Cancelled || active.Status == DownloadStatus.Failed)
        {
            // 移动到已结束列表
            var completedItem = ToItem(active);
            
            // 设置按钮可见性
            completedItem.ShowRetryButton = active.Status == DownloadStatus.Cancelled || active.Status == DownloadStatus.Failed;
            completedItem.ShowOpenFolderButton = active.Status == DownloadStatus.Completed;
            completedItem.InstallPath = active.InstallDir; // 保存路径用于打开目录
            
            // 添加到已结束列表（避免重复）
            var existing = CompletedDownloads.FirstOrDefault(x => x.OfferId == completedItem.OfferId);
            if (existing == null)
            {
                CompletedDownloads.Insert(0, completedItem); // 插入到顶部
                LoggerHelper.Info(I18nHelper.I18n._("Views.DownloadView.MovedToCompleted", completedItem.OfferId, active.Status));
            }
            
            ActiveDownloads.Clear();
            
            // 只在队列变化时更新
            RefreshQueueIfChanged();
            return;
        }
        
        // 更新或创建激活任务的UI项
        if (ActiveDownloads.Count == 0) 
        {
            ActiveDownloads.Add(ToItem(active));
        }
        
        var view = ActiveDownloads[0];
        view.GameName = active.GameName;
        view.OfferId = active.OfferId;
        view.Progress = active.Progress;
        view.DownloadedSize = active.DownloadedSizeDisplay;
        view.TotalSize = active.TotalSizeDisplay;
        view.SpeedDisplay = active.SpeedDisplay;
        view.SpeedTooltip = ToMbps(active.SpeedBytesPerSec);
        view.EstimatedTime = FormatEta(active.EstimatedSecondsLeft);
        view.IsPaused = active.Status == DownloadStatus.Paused;
        
        // 更新状态
        var (newStatus, newIcon) = MapStatusAndIcon(active.Status);
        view.StatusText = newStatus;
        view.StatusIcon = newIcon;
        
        view.CanPause = active.CanPause;
        view.CanResume = active.CanResume;
        view.CanCancel = active.CanCancel;

        // 只在队列变化时更新（避免每500ms重建队列）
        RefreshQueueIfChanged();
    }

    /// <summary>
    /// 只在队列实际变化时刷新队列UI
    /// </summary>
    private void RefreshQueueIfChanged()
    {
        var currentQueue = DownloadHelper.GetQueue().ToList();
        var currentCount = currentQueue.Count;
        var currentIds = new HashSet<string>(currentQueue.Select(t => t.OfferId));

        // 检查队列是否变化（数量或成员变化）
        bool queueChanged = currentCount != _lastQueueCount || 
                           !currentIds.SetEquals(_lastQueueIds);

        if (queueChanged)
        {
            // 重建队列UI
            QueuedDownloads.Clear();
            foreach (var q in currentQueue)
            {
                QueuedDownloads.Add(ToItem(q));
            }

            // 更新缓存
            _lastQueueCount = currentCount;
            _lastQueueIds = currentIds;
        }
    }

    private (string text, string icon) MapStatusAndIcon(DownloadStatus s) => s switch
    {
        DownloadStatus.Waiting => (I18nHelper.I18n._("Views.DownloadView.StatusWaiting"), "\uE8B7"),
        DownloadStatus.Paused => (I18nHelper.I18n._("Views.DownloadView.StatusPaused"), "\uE769"),
        DownloadStatus.Downloading => (I18nHelper.I18n._("Views.DownloadView.StatusDownloading"), "\uE896"),
        DownloadStatus.Installing => (I18nHelper.I18n._("Views.DownloadView.StatusInstalling"), "\uE8B1"),
        DownloadStatus.InstallingRuntime => (I18nHelper.I18n._("Views.DownloadView.StatusInstallingRuntime"), "\uE8B1"),
        DownloadStatus.Failed => (I18nHelper.I18n._("Views.DownloadView.StatusFailed"), "\uE783"),
        DownloadStatus.Cancelled => (I18nHelper.I18n._("Views.DownloadView.StatusCancelled"), "\uE711"),
        DownloadStatus.Completed => (I18nHelper.I18n._("Views.DownloadView.StatusCompleted"), "\uE73E"),
        _ => (string.Empty, "\uE896")
    };

    private string MapStatus(DownloadStatus s) => MapStatusAndIcon(s).text;

    private string FormatEta(double seconds)
    {
        if (double.IsNaN(seconds) || seconds <= 0) return I18nHelper.I18n._("Views.DownloadView.EtaUnknown");
        var ts = TimeSpan.FromSeconds(seconds);
        var prefix = I18nHelper.I18n._("Views.DownloadView.EtaPrefix");
        if (ts.TotalHours >= 1) return $"{prefix} {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{prefix} {ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private string ToMbps(double bytesPerSec)
    {
        if (bytesPerSec <= 0) return "0 Mbps";
        var mbps = bytesPerSec * 8 / (1024 * 1024);
        return $"{mbps:0.0} Mbps";
    }

    private void SyncActive()
    {
        ActiveDownloads.Clear();
        var active = DownloadHelper.GetActive();
        if (active != null)
        {
            SubscribeToActiveTask(active);
            ActiveDownloads.Add(ToItem(active));
        }
        
        // 强制刷新队列（初始化时）
        var currentQueue = DownloadHelper.GetQueue().ToList();
        _lastQueueCount = currentQueue.Count;
        _lastQueueIds = new HashSet<string>(currentQueue.Select(t => t.OfferId));
        
        QueuedDownloads.Clear();
        foreach (var q in currentQueue)
        {
            QueuedDownloads.Add(ToItem(q));
        }
    }

    private DownloadItemModel ToItem(DownloadTaskModel task)
    {
        var active = DownloadHelper.GetActive();
        bool isActiveTask = active != null && active.OfferId == task.OfferId;
        
        var (statusText, statusIcon) = MapStatusAndIcon(task.Status);
        
        return new DownloadItemModel
        {
            GameName = task.GameName,
            OfferId = task.OfferId,
            Progress = task.Progress,
            DownloadedSize = task.DownloadedSizeDisplay,
            TotalSize = task.TotalSizeDisplay,
            SpeedDisplay = task.SpeedDisplay,
            SpeedTooltip = ToMbps(task.SpeedBytesPerSec),
            EstimatedTime = FormatEta(task.EstimatedSecondsLeft),
            // 只有激活任务才根据状态判断是否暂停，队列任务始终显示为未暂停（下载图标）
            IsPaused = isActiveTask ? (task.Status == DownloadStatus.Paused) : false,
            StatusText = statusText,
            StatusIcon = statusIcon,
            CanPause = task.CanPause,
            CanResume = task.CanResume,
            CanCancel = task.CanCancel
        };
    }

    private void ApplyLimit()
    {
        if (CheckBox_GlobalLimit.IsChecked == true && double.TryParse(TextBox_GlobalLimit.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var mbps))
            DownloadHelper.SetGlobalLimitMbps(mbps);
        else
            DownloadHelper.SetGlobalLimitMbps(null);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class DownloadItemModel : INotifyPropertyChanged
{
    private double _progress;
    public double Progress { get => _progress; set { _progress = value; OnPropertyChanged(); } }

    private string _gameName;
    public string GameName { get => _gameName; set { _gameName = value; OnPropertyChanged(); } }

    private string _offerId;
    public string OfferId { get => _offerId; set { _offerId = value; OnPropertyChanged(); } }

    private string _downloadedSize;
    public string DownloadedSize { get => _downloadedSize; set { _downloadedSize = value; OnPropertyChanged(); } }

    private string _totalSize;
    public string TotalSize { get => _totalSize; set { _totalSize = value; OnPropertyChanged(); } }

    private string _speedDisplay;
    public string SpeedDisplay { get => _speedDisplay; set { _speedDisplay = value; OnPropertyChanged(); } }

    private string _speedTooltip;
    public string SpeedTooltip { get => _speedTooltip; set { _speedTooltip = value; OnPropertyChanged(); } }

    private string _estimatedTime;
    public string EstimatedTime { get => _estimatedTime; set { _estimatedTime = value; OnPropertyChanged(); } }

    private bool _isPaused;
    public bool IsPaused { get => _isPaused; set { _isPaused = value; OnPropertyChanged(); } }

    private string _statusText;
    public string StatusText 
    { 
        get => _statusText; 
        set 
        { 
            if (_statusText != value)
            {
                _statusText = value; 
                OnPropertyChanged();
            }
        } 
    }

    private string _statusIcon = "\uE896"; // 默认下载图标
    public string StatusIcon
    {
        get => _statusIcon;
        set
        {
            if (_statusIcon != value)
            {
                _statusIcon = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _canPause = true;
    public bool CanPause { get => _canPause; set { _canPause = value; OnPropertyChanged(); } }

    private bool _canResume;
    public bool CanResume { get => _canResume; set { _canResume = value; OnPropertyChanged(); } }

    private bool _canCancel = true;
    public bool CanCancel { get => _canCancel; set { _canCancel = value; OnPropertyChanged(); } }

    // 新增：按钮可见性控制
    private bool _showRetryButton;
    public bool ShowRetryButton { get => _showRetryButton; set { _showRetryButton = value; OnPropertyChanged(); } }

    private bool _showOpenFolderButton;
    public bool ShowOpenFolderButton { get => _showOpenFolderButton; set { _showOpenFolderButton = value; OnPropertyChanged(); } }

    // 新增：保存安装路径
    public string InstallPath { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
