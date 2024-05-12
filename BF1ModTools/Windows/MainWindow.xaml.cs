using BF1ModTools.Api;
using BF1ModTools.Core;
using BF1ModTools.Utils;
using BF1ModTools.Helper;
using CommunityToolkit.Mvvm.Input;
using Hardcodet.Wpf.TaskbarNotification;

namespace BF1ModTools.Windows;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// 窗口关闭识别标志
    /// </summary>
    public static bool IsCodeClose { get; set; } = false;
    /// <summary>
    /// 第一次通知标志
    /// </summary>
    private bool _isFirstNotice = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private async void Window_Main_Loaded(object sender, RoutedEventArgs e)
    {
        LoggerHelper.Info("启动主程序成功");

        Title = $"战地1模组工具箱 v{CoreUtil.VersionInfo} - {CoreUtil.GetIsAdminStr()}";

        // 重置窗口关闭标志
        IsCodeClose = false;

        // 初始化工作
        Ready.Run();

        // 检查更新（放到最后执行）
        await CheckUpdate();
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    private void Window_Main_Closing(object sender, CancelEventArgs e)
    {
        // 当用户从UI点击关闭时才执行
        if (!IsCodeClose)
        {
            // 取消关闭事件，隐藏主窗口
            e.Cancel = true;
            this.Hide();

            // 仅第一次通知
            if (!_isFirstNotice)
            {
                NotifyIcon_Main.ShowBalloonTip("战地1模组工具箱 已最小化到托盘", "可通过托盘右键菜单完全退出程序", BalloonIcon.Info);
                _isFirstNotice = true;
            }

            return;
        }

        // 清理工作
        Ready.Stop();

        // 释放托盘图标
        NotifyIcon_Main?.Dispose();
        NotifyIcon_Main = null;

        LoggerHelper.Info("关闭主程序成功");
    }

    /// <summary>
    /// 超链接请求导航事件
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        ProcessHelper.OpenLink(e.Uri.OriginalString);
        e.Handled = true;
    }

    /// <summary>
    /// 检查更新
    /// </summary>
    private async Task CheckUpdate()
    {
        LoggerHelper.Info("正在检测新版本中...");
        NotifierHelper.Notice("正在检测新版本中...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                LoggerHelper.Error("检测新版本失败，请检查网络连接");
                NotifierHelper.Error("检测新版本失败，请检查网络连接");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Warn($"检测新版本失败，开始第 {i} 次重试中...");
            }

            var webVersion = await CoreApi.GetWebUpdateVersion();
            if (webVersion is not null)
            {
                if (CoreUtil.VersionInfo >= webVersion)
                {
                    LoggerHelper.Info($"恭喜，当前是最新版本 {CoreUtil.VersionInfo}");
                    NotifierHelper.Info($"恭喜，当前是最新版本 {CoreUtil.VersionInfo}");
                    return;
                }

                IconHyperlink_Update.Text = $"发现新版本 v{webVersion}，点击下载更新";
                IconHyperlink_Update.Visibility = Visibility.Visible;

                LoggerHelper.Info($"发现最新版本，请前往官网下载最新版本 {webVersion}");
                NotifierHelper.Warning($"发现最新版本，请前往官网下载最新版本 {webVersion}");
                return;
            }
        }
    }

    /// <summary>
    /// 显示主窗口
    /// </summary>
    [RelayCommand]
    private void ShowWindow()
    {
        this.Show();

        if (this.WindowState == WindowState.Minimized)
            this.WindowState = WindowState.Normal;

        this.Activate();
        this.Focus();
    }

    /// <summary>
    /// 切换账号窗口
    /// </summary>
    [RelayCommand]
    private void SwitchAccount()
    {
        var accountWindow = new AccountWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = accountWindow;
        // 设置关闭标志
        IsCodeClose = true;
        // 关闭主窗口
        this.Close();

        // 显示更换账号窗口
        accountWindow.Show();
    }

    /// <summary>
    /// 退出程序
    /// </summary>
    [RelayCommand]
    private void ExitApp()
    {
        // 设置关闭标志
        IsCodeClose = true;
        this.Close();
    }
}