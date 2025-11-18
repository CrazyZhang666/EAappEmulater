using EAappEmulater.Enums;
using EAappEmulater.Helper;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EAappEmulater.Models;

public class DownloadTaskModel : INotifyPropertyChanged
{
    public string OfferId { get; set; }
    public string GameName { get; set; }
    public string InstallDir { get; set; }
    public string InstallLanguage { get; set; }
    public string JitUrl { get; set; }

    private DownloadStatus _status = DownloadStatus.Waiting;
    public DownloadStatus Status 
    { 
        get => _status; 
        set 
        { 
            if (_status != value)
            {
                _status = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(CanPause)); 
                OnPropertyChanged(nameof(CanResume)); 
                OnPropertyChanged(nameof(CanCancel)); 
            }
        } 
    }

    private double _progress;
    public double Progress { get => _progress; set { _progress = value; OnPropertyChanged(); } }

    private long _downloadedBytes;
    public long DownloadedBytes { get => _downloadedBytes; private set { _downloadedBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(DownloadedSizeDisplay)); OnPropertyChanged(nameof(EstimatedSecondsLeft)); } }

    public void AddDownloadedBytes(long add)
    {
        long newVal = Interlocked.Add(ref _downloadedBytes, add);
        OnPropertyChanged(nameof(DownloadedBytes));
        OnPropertyChanged(nameof(DownloadedSizeDisplay));
        OnPropertyChanged(nameof(EstimatedSecondsLeft));
    }

    public long TotalBytes { get; set; }

    private double _speedBytesPerSec;
    public double SpeedBytesPerSec { get => _speedBytesPerSec; set { _speedBytesPerSec = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedDisplay)); OnPropertyChanged(nameof(EstimatedSecondsLeft)); } }

    public string DownloadedSizeDisplay => FormatBytes(DownloadedBytes);
    public string TotalSizeDisplay => FormatBytes(TotalBytes);
    public string SpeedDisplay => (SpeedBytesPerSec <= 0 ? "0.00 B/s" : FormatBytes((long)SpeedBytesPerSec) + "/s");

    public double EstimatedSecondsLeft => (SpeedBytesPerSec > 0 && TotalBytes > DownloadedBytes) ? (TotalBytes - DownloadedBytes) / SpeedBytesPerSec : double.NaN;

    public CancellationTokenSource TokenSource { get; set; }
    public volatile bool RequestPause;
    public volatile bool RequestCancel;
    private volatile bool _isStopping; // Flag to prevent double-clicks during stop

    public bool CanPause => Status == DownloadStatus.Downloading && !_isStopping;
    public bool CanResume => (Status == DownloadStatus.Paused || Status == DownloadStatus.Waiting) && !_isStopping;
    public bool CanCancel => !_isStopping;

    public int ConsecutiveFailures { get; set; }

    public void Pause()
    {
        if (_isStopping) return;
        _isStopping = true;
        RequestPause = true;
        LoggerHelper.Info($"TaskModel.Pause called for {OfferId}, RequestPause={RequestPause}, RequestCancel={RequestCancel}, TokenSourceCancelled={(TokenSource?.IsCancellationRequested.ToString() ?? "null")}, Status={Status}");
        TokenSource?.Cancel();
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
        OnPropertyChanged(nameof(CanCancel));
    }

    public void Resume()
    {
        LoggerHelper.Info($"TaskModel.Resume called, current RequestPause: {RequestPause}, Status: {Status}");
        RequestPause = false;
        RequestCancel = false;
        _isStopping = false;
        TokenSource = new CancellationTokenSource();
        Status = DownloadStatus.Waiting;
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
        OnPropertyChanged(nameof(CanCancel));
        LoggerHelper.Info($"TaskModel.Resume finished, new Status: {Status}");
    }

    public void Cancel()
    {
        if (_isStopping) return;
        _isStopping = true;
        RequestCancel = true;
        LoggerHelper.Info($"TaskModel.Cancel called for {OfferId}, RequestCancel={RequestCancel}, RequestPause={RequestPause}, TokenSourceCancelled={(TokenSource?.IsCancellationRequested.ToString() ?? "null")}, Status={Status}");
        TokenSource?.Cancel();
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
        OnPropertyChanged(nameof(CanCancel));
    }

    public void ResetStoppingFlag()
    {
        _isStopping = false;
        LoggerHelper.Debug($"TaskModel.ResetStoppingFlag for {OfferId}");
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
        OnPropertyChanged(nameof(CanCancel));
    }

    private static string FormatBytes(long bytes)
    {
        double v = bytes; string unit = "B";
        if (v >= 1024) { v /= 1024; unit = "KB"; }
        if (v >= 1024) { v /= 1024; unit = "MB"; }
        if (v >= 1024) { v /= 1024; unit = "GB"; }
        if (v >= 1024) { v /= 1024; unit = "TB"; }
        return $"{v:0.00} {unit}";
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}