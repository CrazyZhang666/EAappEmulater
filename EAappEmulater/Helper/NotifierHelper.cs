using Notification.Wpf;
using Notification.Wpf.Constants;
using Notification.Wpf.Controls;

namespace EAappEmulater.Helper;

public static class NotifierHelper
{
    private static readonly NotificationManager _notificationManager = new();

    private static readonly TimeSpan _expirationTime = TimeSpan.FromSeconds(3);

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
    /// 显示Toast通知
    /// </summary>
    private static void Show(NotificationType type, string message)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var title = type switch
            {
                NotificationType.None => "",
                NotificationType.Information => "信息 Information",
                NotificationType.Success => "成功 Success",
                NotificationType.Warning => "警告 Warning",
                NotificationType.Error => "错误 Error",
                NotificationType.Notification => "通知 Notice",
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

    public static void Info(string message)
    {
        Show(NotificationType.Information, message);
    }

    public static void Success(string message)
    {
        Show(NotificationType.Success, message);
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