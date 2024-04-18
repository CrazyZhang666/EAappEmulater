using BF1ModTools.Api;
using BF1ModTools.Core;
using BF1ModTools.Utils;
using BF1ModTools.Helper;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Navigation;

namespace BF1ModTools;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow
{
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

        Title = $"战地1模组工具箱 v{CoreUtil.VersionInfo}";

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
        // 清理工作
        Ready.Stop();

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

                LoggerHelper.Info($"发现最新版本，请前往官网下载最新版本 {webVersion}");

                if (MessageBox.Show("发现最新版本，请前往官网下载最新版本\nhttps://battlefield.vip",
                    "版本更新", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    ProcessHelper.OpenLink("https://battlefield.vip");
                    return;
                }
            }
        }
    }

    [RelayCommand]
    private async Task SelectBf1Dir()
    {
        await CoreUtil.GetBf1InstallPath(true);
    }

    [RelayCommand]
    private void ChangeLoginAccount()
    {
        if (MessageBox.Show("你确定要更换当前登录账号吗？",
            "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
        {
            Account.Reset();

            var loadWindow = new LoadWindow
            {
                IsLogout = true
            };

            // 转移主程序控制权
            Application.Current.MainWindow = loadWindow;
            // 关闭主窗窗口
            this.Close();

            // 显示初始化窗口
            loadWindow.Show();
        }
    }
}