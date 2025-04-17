using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Api;
using EAappEmulater.Core;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using EAappEmulater.Utils;
using RestSharp;

namespace EAappEmulater.Windows;

/// <summary>
/// LoadWindow.xaml 的交互逻辑
/// </summary>
public partial class LoadWindow
{
    public LoadWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private void Window_Load_Loaded(object sender, RoutedEventArgs e)
    {
    }

    /// <summary>
    /// 窗口内容呈现完毕后事件
    /// </summary>
    private async void Window_Load_ContentRendered(object sender, EventArgs e)
    {
        // 读取账号配置文件
        Account.Read();
        // 开始验证Cookie有效性
        await CheckCookie();
    }

    /// <summary>
    /// 窗口关闭时事件
    /// </summary>
    private void Window_Load_Closing(object sender, CancelEventArgs e)
    {
    }

    /// <summary>
    /// 显示加载状态到UI界面
    /// </summary>
    private void DisplayLoadState(string log)
    {
        TextBlock_CheckState.Text = log;
    }

    /// <summary>
    /// 检查Cookie信息
    /// </summary>
    private async Task CheckCookie()
    {
        DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.CheckCookieValidity"));
        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.CheckCookieValidity"));

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.CheckCookieValidityError"));
                LoggerHelper.Error(I18nHelper.I18n._("Windows.LoadWindow.CheckCookieValidityError"));
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.CheckCookieValidityErrorRetry", i));
                LoggerHelper.Warn(I18nHelper.I18n._("Windows.LoadWindow.CheckCookieValidityErrorRetry", i));
            }

            var result = await EaApi.GetToken();
            // 代表请求完成，排除超时情况
            if (result != null && result.IsSuccess)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.CheckCookieValiditySuccess"));
                // 如果Cookie有效，则开始初始化
                await InitGameInfo();

                return;
            }
            else if (result != null && !result.IsSuccess)
            {
                var loginWindow = new LoginWindow(false, result.Content);

                // 转移主程序控制权
                Application.Current.MainWindow = loginWindow;
                // 关闭当前窗口
                this.Close();

                // 显示登录窗口
                loginWindow.Show();
                return;
            }
            else {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.CheckCookieValidityErrorStop"));
                LoggerHelper.Error(I18nHelper.I18n._("Windows.LoadWindow.CheckCookieValidityErrorStop"));
            }
            
        }
    }

    /// <summary>
    /// 初始化游戏信息
    /// </summary>
    private async Task InitGameInfo()
    {
        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.InitGameInfo"));

        // 关闭服务进程
        CoreUtil.CloseServiceProcess();

        DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.CloseServiceProcess"));
        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.CloseServiceProcess"));
        FileHelper.ExtractResFile("Exec.OriginDebug.exe", CoreUtil.File_Service_OriginDebug);

        /////////////////////////////////////////////////

        DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.GetRegistryInfo"));
        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.GetRegistryInfo"));
        // 从注册表获取游戏安装信息
        foreach (var gameInfo in Base.GameInfoDb)
        {
            LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.GetRegistryGameInfo", gameInfo.Value.Name));

            var installDir = RegistryHelper.GetRegistryInstallDir(gameInfo.Value.Regedit);
            if (!string.IsNullOrWhiteSpace(installDir))
            {
                gameInfo.Value.Dir = installDir;
                gameInfo.Value.IsInstalled = true;
                LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.GetRegistryGameInfoSuccess", gameInfo.Value.Name));
            }
            else
            {
                installDir = RegistryHelper.GetRegistryInstallDir(gameInfo.Value.Regedit2);
                if (!string.IsNullOrWhiteSpace(installDir))
                {
                    gameInfo.Value.Dir = installDir;
                    gameInfo.Value.IsInstalled = true;
                    LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.GetRegistry2GameInfoSuccess", gameInfo.Value.Name));
                }
            }

            if (!gameInfo.Value.IsInstalled)
                LoggerHelper.Warn(I18nHelper.I18n._("Windows.LoadWindow.GetRegistryGameInfoNotFound", gameInfo.Value.Name));
        }

        /////////////////////////////////////////////////

        DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.RefreshBaseTokenProcess"));
        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.RefreshBaseTokenProcess"));

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.RefreshBaseTokenErrorNetwork"));
                LoggerHelper.Error(I18nHelper.I18n._("Windows.LoadWindow.RefreshBaseTokenErrorNetwork"));
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.RefreshBaseTokenRetry", i));
                LoggerHelper.Warn(I18nHelper.I18n._("Windows.LoadWindow.RefreshBaseTokenRetry", i));
            }

            if (await Ready.RefreshBaseTokens())
            {
                LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.RefreshBaseTokenSuccess"));
                break;
            }
        }

        DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.GetLoginAccountInfoProcess"));
        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.GetLoginAccountInfoProcess"));

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.GetLoginAccountInfoErrorNetwork"));
                LoggerHelper.Error(I18nHelper.I18n._("Windows.LoadWindow.GetLoginAccountInfoErrorNetwork"));
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.GetLoginAccountInfoRetry", i));
                LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.GetLoginAccountInfoRetry", i));
            }

            if (await Ready.GetLoginAccountInfo())
            {
                LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.GetLoginAccountInfoSuccess"));
                break;
            }
        }

        /////////////////////////////////////////////////

        // 保存账号配置文件
        Account.Write();

        DisplayLoadState(I18nHelper.I18n._("Windows.LoadWindow.StartMainProcess"));
        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoadWindow.StartMainProcess"));

        var mainWindow = new MainWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = mainWindow;
        // 关闭当前窗口
        this.Close();

        // 显示主窗口
        mainWindow.Show();
    }

    /// <summary>
    /// 打开配置文件
    /// </summary>
    [RelayCommand]
    private void OpenConfigFolder()
    {
        ProcessHelper.OpenDirectory(CoreUtil.Dir_Default);
    }

    /// <summary>
    /// 打开账号切换窗口
    /// </summary>
    [RelayCommand]
    private void RunAccountWindow()
    {
        var accountWindow = new AccountWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = accountWindow;
        // 关闭当前窗口
        this.Close();

        // 显示切换账号窗口
        accountWindow.Show();
    }

    /// <summary>
    /// 退出程序
    /// </summary>
    [RelayCommand]
    private void ExitApplication()
    {
        Application.Current.Shutdown();
    }
}
