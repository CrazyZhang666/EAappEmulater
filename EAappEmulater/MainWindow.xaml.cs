using EAappEmulater.Api;
using EAappEmulater.Core;
using EAappEmulater.Utils;
using EAappEmulater.Views;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using CommunityToolkit.Mvvm.Input;

namespace EAappEmulater;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// 导航字典
    /// </summary>
    private readonly Dictionary<string, UserControl> NavDictionary = new();

    /// <summary>
    /// 用于向外暴露主窗口实例
    /// </summary>
    public static Window MainWindowInstance { get; private set; }

    public MainModel MainModel { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();

        CreateView();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Window_Main_Loaded(object sender, RoutedEventArgs e)
    {
        LoggerHelper.Info("启动主程序成功");

        // 向外暴露主窗口实例
        MainWindowInstance = this;

        // 首页导航
        Navigate(NavDictionary.First().Key);

        // 初始化工作
        Ready.Run();

        // 显示当前玩家登录账号
        MainModel.Avatar = Account.Avatar;
        MainModel.PlayerName = Account.PlayerName;
        MainModel.PersonaId = Account.PersonaId;

        // 检查更新（放到最后执行）
        await CheckUpdate();
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Main_Closing(object sender, CancelEventArgs e)
    {
        // 清理工作
        Ready.Stop();

        LoggerHelper.Info("关闭主程序成功");
    }

    /// <summary>
    /// 创建页面
    /// </summary>
    private void CreateView()
    {
        NavDictionary.Add("GameView", new GameView());
        NavDictionary.Add("Game2View", new Game2View());
        NavDictionary.Add("LogView", new LogView());
        NavDictionary.Add("AboutView", new AboutView());

        NavDictionary.Add("AccountView", new AccountView());
        NavDictionary.Add("SettingView", new SettingView());
    }

    /// <summary>
    /// View页面导航
    /// </summary>
    [RelayCommand]
    private void Navigate(string viewName)
    {
        if (!NavDictionary.ContainsKey(viewName))
            return;

        if (ContentControl_NavRegion.Content == NavDictionary[viewName])
            return;

        ContentControl_NavRegion.Content = NavDictionary[viewName];
    }

    /// <summary>
    /// 检查更新
    /// </summary>
    private async Task CheckUpdate()
    {
        LoggerHelper.Info("正在检测新版本中...");
        NotifierHelper.Notice("正在检测新版本中...");

        var version = await EaApi.GetWebUpdateVersion();
        if (version is null)
        {
            LoggerHelper.Warn("检测新版本失败");
            NotifierHelper.Warning("检测新版本失败");
            return;
        }

        if (CoreUtil.VersionInfo == version)
        {
            LoggerHelper.Info($"恭喜，当前是最新版本 {version}");
            NotifierHelper.Info($"恭喜，当前是最新版本 {version}");
            return;
        }

        if (MessageBox.Show("发现最新版本，请前往官网下载最新版本\nhttps://battlefield.vip",
            "版本更新", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
        {
            ProcessHelper.OpenLink("https://battlefield.vip");
            return;
        }
    }
}