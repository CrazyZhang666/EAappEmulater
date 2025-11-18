using Notification.Wpf;
using Notification.Wpf.Constants;
using Notification.Wpf.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace EAappEmulater.Helper;

public static class NotifierHelper
{
    private static readonly NotificationManager _notificationManager = new();

    private static readonly TimeSpan _expirationTime = TimeSpan.FromSeconds(3);
    
    /// <summary>
    /// 托盘图标引用（用于Windows系统通知）
    /// </summary>
    private static TaskbarIcon _taskbarIcon;

    static NotifierHelper()
    {
        NotificationConstants.DefaultRowCounts = 1;
        NotificationConstants.NotificationsOverlayWindowMaxCount = 3;
        NotificationConstants.MessagePosition = NotificationPosition.BottomCenter;

        NotificationConstants.MinWidth = 360.0;
        NotificationConstants.MaxWidth = NotificationConstants.MinWidth;

        NotificationConstants.FontName = "微软雅黑";
        NotificationConstants.TitleSize = 12.0;
        NotificationConstants.MessageSize = 12.0;
        NotificationConstants.MessageTextAlignment = TextAlignment.Left;
        NotificationConstants.TitleTextAlignment = TextAlignment.Left;

        NotificationConstants.DefaultForegroundColor = (Brush)new BrushConverter().ConvertFrom("#FFFFFF");
        NotificationConstants.DefaultBackgroundColor = (Brush)new BrushConverter().ConvertFrom("#4C4A48");

        NotificationConstants.InformationBackgroundColor = (Brush)new BrushConverter().ConvertFrom("#0078D4");
        NotificationConstants.SuccessBackgroundColor = (Brush)new BrushConverter().ConvertFrom("#107C10");
        NotificationConstants.WarningBackgroundColor = (Brush)new BrushConverter().ConvertFrom("#FF8C00");
        NotificationConstants.ErrorBackgroundColor = (Brush)new BrushConverter().ConvertFrom("#DA3B01");
    }

    /// <summary>
    /// 设置托盘图标引用
    /// </summary>
    public static void SetTaskbarIcon(TaskbarIcon taskbarIcon)
    {
        _taskbarIcon = taskbarIcon;
    }

    /// <summary>
    /// 显示Toast通知
    /// </summary>
    private static void Show(NotificationType type, string message)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var title = type switch
            {
                NotificationType.None => "",
                NotificationType.Information => I18nHelper.I18n._("Helper.NotifierHelper.Information"),
                NotificationType.Success => I18nHelper.I18n._("Helper.NotifierHelper.Success"),
                NotificationType.Warning => I18nHelper.I18n._("Helper.NotifierHelper.Warning"),
                NotificationType.Error => I18nHelper.I18n._("Helper.NotifierHelper.Error"),
                NotificationType.Notification => I18nHelper.I18n._("Helper.NotifierHelper.Notice"),
                _ => "",
            };

            var content = new NotificationContent
            {
                Title = title,
                Type = type,
                Message = message,
                TrimType = NotificationTextTrimType.Trim
            };

            _notificationManager.Show(content, "MainWindowArea", _expirationTime, null, null, true, false);
        });
    }

    /// <summary>
    /// 显示可点击的Toast通知（带回调）
    /// </summary>
    private static void ShowClickable(NotificationType type, string message, Action onClick)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var title = type switch
            {
                NotificationType.None => "",
                NotificationType.Information => I18nHelper.I18n._("Helper.NotifierHelper.Information"),
                NotificationType.Success => I18nHelper.I18n._("Helper.NotifierHelper.Success"),
                NotificationType.Warning => I18nHelper.I18n._("Helper.NotifierHelper.Warning"),
                NotificationType.Error => I18nHelper.I18n._("Helper.NotifierHelper.Error"),
                NotificationType.Notification => I18nHelper.I18n._("Helper.NotifierHelper.Notice"),
                _ => "",
            };

            var content = new NotificationContent
            {
                Title = title,
                Type = type,
                Message = message,
                TrimType = NotificationTextTrimType.Trim
            };

            _notificationManager.Show(content, "MainWindowArea", _expirationTime, onClick, null, true, false);
        });
    }

    /// <summary>
    /// 显示Windows系统通知（可点击）
    /// </summary>
    private static void ShowSystemNotification(string title, string message, BalloonIcon icon, Action onClick = null)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (_taskbarIcon == null)
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Helper.NotifierHelper.TaskbarIconNull"));
                // 回退到Toast通知
                Show(NotificationType.Success, message);
                return;
            }

            // 如果有点击回调，注册事件
            if (onClick != null)
            {
                // 使用正确的RoutedEventHandler类型
                RoutedEventHandler balloonClickHandler = null;
                balloonClickHandler = (s, e) =>
                {
                    onClick?.Invoke();
                    // 取消订阅，避免重复触发
                    if (_taskbarIcon != null)
                    {
                        _taskbarIcon.TrayBalloonTipClicked -= balloonClickHandler;
                    }
                };
                
                _taskbarIcon.TrayBalloonTipClicked += balloonClickHandler;
            }

            _taskbarIcon.ShowBalloonTip(title, message, icon);
        });
    }

    public static void Info(string message)
    {
        Show(NotificationType.Information, message);
    }

    public static void Success(string message)
    {
        Show(NotificationType.Success, message);
    }

    /// <summary>
    /// 显示成功通知（可点击）
    /// </summary>
    public static void SuccessClickable(string message, Action onClick)
    {
        ShowClickable(NotificationType.Success, message, onClick);
    }

    /// <summary>
    /// 显示Windows系统成功通知（可点击）
    /// </summary>
    public static void SystemSuccessClickable(string title, string message, Action onClick)
    {
        ShowSystemNotification(title, message, BalloonIcon.Info, onClick);
    }

    public static void Warning(string message)
    {
        Show(NotificationType.Warning, message);
    }

    public static void Error(string message)
    {
        Show(NotificationType.Error, message);
    }

    public static void Notice(string message)
    {
        Show(NotificationType.Notification, message);
    }
}
