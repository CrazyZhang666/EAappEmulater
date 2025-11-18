using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModernWpf.Controls;

[TemplatePart(Name = "PART_ButtonPauseResume", Type = typeof(Button))]
[TemplatePart(Name = "PART_ButtonCancel", Type = typeof(Button))]
public class DownloadGameItem : Control
{
    static DownloadGameItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DownloadGameItem), new FrameworkPropertyMetadata(typeof(DownloadGameItem)));
    }

    public string GameName { get => (string)GetValue(GameNameProperty); set => SetValue(GameNameProperty, value); }
    public static readonly DependencyProperty GameNameProperty = DependencyProperty.Register("GameName", typeof(string), typeof(DownloadGameItem), new PropertyMetadata(string.Empty));

    public string OfferId { get => (string)GetValue(OfferIdProperty); set => SetValue(OfferIdProperty, value); }
    public static readonly DependencyProperty OfferIdProperty = DependencyProperty.Register("OfferId", typeof(string), typeof(DownloadGameItem), new PropertyMetadata(string.Empty));

    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register("Progress", typeof(double), typeof(DownloadGameItem), new PropertyMetadata(0.0));

    public string StatusText { get => (string)GetValue(StatusTextProperty); set => SetValue(StatusTextProperty, value); }
    // 默认状态文本设置为空，避免默认显示为 "Downloading"
    public static readonly DependencyProperty StatusTextProperty = DependencyProperty.Register("StatusText", typeof(string), typeof(DownloadGameItem), new PropertyMetadata(string.Empty));

    public string StatusIcon { get => (string)GetValue(StatusIconProperty); set => SetValue(StatusIconProperty, value); }
    public static readonly DependencyProperty StatusIconProperty = DependencyProperty.Register("StatusIcon", typeof(string), typeof(DownloadGameItem), new PropertyMetadata("\uE896")); // 默认下载图标

    public bool IsPaused { get => (bool)GetValue(IsPausedProperty); set => SetValue(IsPausedProperty, value); }
    public static readonly DependencyProperty IsPausedProperty = DependencyProperty.Register("IsPaused", typeof(bool), typeof(DownloadGameItem), new PropertyMetadata(false));

    public string DownloadedSize { get => (string)GetValue(DownloadedSizeProperty); set => SetValue(DownloadedSizeProperty, value); }
    public static readonly DependencyProperty DownloadedSizeProperty = DependencyProperty.Register("DownloadedSize", typeof(string), typeof(DownloadGameItem), new PropertyMetadata("0 GB"));

    public string TotalSize { get => (string)GetValue(TotalSizeProperty); set => SetValue(TotalSizeProperty, value); }
    public static readonly DependencyProperty TotalSizeProperty = DependencyProperty.Register("TotalSize", typeof(string), typeof(DownloadGameItem), new PropertyMetadata("0 GB"));

    public string SpeedDisplay { get => (string)GetValue(SpeedDisplayProperty); set => SetValue(SpeedDisplayProperty, value); }
    public static readonly DependencyProperty SpeedDisplayProperty = DependencyProperty.Register("SpeedDisplay", typeof(string), typeof(DownloadGameItem), new PropertyMetadata("0 MB/s"));

    public string SpeedTooltip { get => (string)GetValue(SpeedTooltipProperty); set => SetValue(SpeedTooltipProperty, value); }
    public static readonly DependencyProperty SpeedTooltipProperty = DependencyProperty.Register("SpeedTooltip", typeof(string), typeof(DownloadGameItem), new PropertyMetadata("0 Mbps"));

    public bool IsLimitSpeed { get => (bool)GetValue(IsLimitSpeedProperty); set => SetValue(IsLimitSpeedProperty, value); }
    public static readonly DependencyProperty IsLimitSpeedProperty = DependencyProperty.Register("IsLimitSpeed", typeof(bool), typeof(DownloadGameItem), new PropertyMetadata(false));

    public string SpeedLimit { get => (string)GetValue(SpeedLimitProperty); set => SetValue(SpeedLimitProperty, value); }
    public static readonly DependencyProperty SpeedLimitProperty = DependencyProperty.Register("SpeedLimit", typeof(string), typeof(DownloadGameItem), new PropertyMetadata("10"));

    // Use glyphs for icons via Segoe MDL2 Assets
    public string PauseResumeIcon { get => (string)GetValue(PauseResumeIconProperty); set => SetValue(PauseResumeIconProperty, value); }
    public static readonly DependencyProperty PauseResumeIconProperty = DependencyProperty.Register("PauseResumeIcon", typeof(string), typeof(DownloadGameItem), new PropertyMetadata("\uE769")); // Download glyph

    public string EstimatedTime { get => (string)GetValue(EstimatedTimeProperty); set => SetValue(EstimatedTimeProperty, value); }
    public static readonly DependencyProperty EstimatedTimeProperty = DependencyProperty.Register("EstimatedTime", typeof(string), typeof(DownloadGameItem), new PropertyMetadata("--"));

    public bool CanPause { get => (bool)GetValue(CanPauseProperty); set => SetValue(CanPauseProperty, value); }
    public static readonly DependencyProperty CanPauseProperty = DependencyProperty.Register("CanPause", typeof(bool), typeof(DownloadGameItem), new PropertyMetadata(true));

    public bool CanResume { get => (bool)GetValue(CanResumeProperty); set => SetValue(CanResumeProperty, value); }
    public static readonly DependencyProperty CanResumeProperty = DependencyProperty.Register("CanResume", typeof(bool), typeof(DownloadGameItem), new PropertyMetadata(false));

    public bool CanCancel { get => (bool)GetValue(CanCancelProperty); set => SetValue(CanCancelProperty, value); }
    public static readonly DependencyProperty CanCancelProperty = DependencyProperty.Register("CanCancel", typeof(bool), typeof(DownloadGameItem), new PropertyMetadata(true));

    public ICommand PauseResumeCommand { get => (ICommand)GetValue(PauseResumeCommandProperty); set => SetValue(PauseResumeCommandProperty, value); }
    public static readonly DependencyProperty PauseResumeCommandProperty = DependencyProperty.Register("PauseResumeCommand", typeof(ICommand), typeof(DownloadGameItem), new PropertyMetadata(default));

    public object PauseResumeCommandParameter { get => GetValue(PauseResumeCommandParameterProperty); set => SetValue(PauseResumeCommandParameterProperty, value); }
    public static readonly DependencyProperty PauseResumeCommandParameterProperty = DependencyProperty.Register("PauseResumeCommandParameter", typeof(object), typeof(DownloadGameItem), new PropertyMetadata(default));

    public ICommand CancelCommand { get => (ICommand)GetValue(CancelCommandProperty); set => SetValue(CancelCommandProperty, value); }
    public static readonly DependencyProperty CancelCommandProperty = DependencyProperty.Register("CancelCommand", typeof(ICommand), typeof(DownloadGameItem), new PropertyMetadata(default));

    public object CancelCommandParameter { get => GetValue(CancelCommandParameterProperty); set => SetValue(CancelCommandParameterProperty, value); }
    public static readonly DependencyProperty CancelCommandParameterProperty = DependencyProperty.Register("CancelCommandParameter", typeof(object), typeof(DownloadGameItem), new PropertyMetadata(default));

    // 新增：重新下载命令
    public ICommand RetryCommand { get => (ICommand)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }
    public static readonly DependencyProperty RetryCommandProperty = DependencyProperty.Register("RetryCommand", typeof(ICommand), typeof(DownloadGameItem), new PropertyMetadata(default));

    public object RetryCommandParameter { get => GetValue(RetryCommandParameterProperty); set => SetValue(RetryCommandParameterProperty, value); }
    public static readonly DependencyProperty RetryCommandParameterProperty = DependencyProperty.Register("RetryCommandParameter", typeof(object), typeof(DownloadGameItem), new PropertyMetadata(default));

    // 新增：打开目录命令
    public ICommand OpenFolderCommand { get => (ICommand)GetValue(OpenFolderCommandProperty); set => SetValue(OpenFolderCommandProperty, value); }
    public static readonly DependencyProperty OpenFolderCommandProperty = DependencyProperty.Register("OpenFolderCommand", typeof(ICommand), typeof(DownloadGameItem), new PropertyMetadata(default));

    public object OpenFolderCommandParameter { get => GetValue(OpenFolderCommandParameterProperty); set => SetValue(OpenFolderCommandParameterProperty, value); }
    public static readonly DependencyProperty OpenFolderCommandParameterProperty = DependencyProperty.Register("OpenFolderCommandParameter", typeof(object), typeof(DownloadGameItem), new PropertyMetadata(default));

    // 新增：控制按钮可见性
    public bool ShowPauseResumeButton { get => (bool)GetValue(ShowPauseResumeButtonProperty); set => SetValue(ShowPauseResumeButtonProperty, value); }
    public static readonly DependencyProperty ShowPauseResumeButtonProperty = DependencyProperty.Register("ShowPauseResumeButton", typeof(bool), typeof(DownloadGameItem), new PropertyMetadata(true));

    public bool ShowCancelButton { get => (bool)GetValue(ShowCancelButtonProperty); set => SetValue(ShowCancelButtonProperty, value); }
    public static readonly DependencyProperty ShowCancelButtonProperty = DependencyProperty.Register("ShowCancelButton", typeof(bool), typeof(DownloadGameItem), new PropertyMetadata(true));

    public bool ShowRetryButton { get => (bool)GetValue(ShowRetryButtonProperty); set => SetValue(ShowRetryButtonProperty, value); }
    public static readonly DependencyProperty ShowRetryButtonProperty = DependencyProperty.Register("ShowRetryButton", typeof(bool), typeof(DownloadGameItem), new PropertyMetadata(false));

    public bool ShowOpenFolderButton { get => (bool)GetValue(ShowOpenFolderButtonProperty); set => SetValue(ShowOpenFolderButtonProperty, value); }
    public static readonly DependencyProperty ShowOpenFolderButtonProperty = DependencyProperty.Register("ShowOpenFolderButton", typeof(bool), typeof(DownloadGameItem), new PropertyMetadata(false));

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        // 不再需要手动绑定 Click 事件，XAML 模板已绑定 Command/CommandParameter
    }
}
