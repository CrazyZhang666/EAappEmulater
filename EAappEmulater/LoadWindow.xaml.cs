using EAappEmulater.Api;
using EAappEmulater.Core;
using EAappEmulater.Utils;
using EAappEmulater.Helper;
using Microsoft.Web.WebView2.Core;

namespace EAappEmulater;

/// <summary>
/// LoadWindow.xaml 的交互逻辑
/// </summary>
public partial class LoadWindow
{
    private const string _host = "https://accounts.ea.com/connect/auth?client_id=sparta-backend-as-user-pc&response_type=code&release_type=none";

    /// <summary>
    /// 是否登出当前账号（用于切换新账号使用）
    /// </summary>
    public bool IsLogout { get; set; } = false;

    public LoadWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private async void Window_Load_Loaded(object sender, RoutedEventArgs e)
    {
        if (await CheckCookie())
        {
            InitGameInfo();
            return;
        }

        await InitWebView2();
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    private void Window_Load_Closing(object sender, CancelEventArgs e)
    {
        WebView2_Main?.Dispose();
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
    private async Task<bool> CheckCookie()
    {
        LoggerHelper.Info("开始初始化游戏信息...");

        // 先读取配置文件
        Globals.Read();

        Grid_Part1.Visibility = Visibility.Visible;
        Grid_Part2.Visibility = Visibility.Hidden;
        Part2_BtnTools.Visibility = Visibility.Hidden;

        DisplayLoadState("正在检测玩家 Cookie 有效性...");
        LoggerHelper.Info("正在检测玩家 Cookie 有效性...");
        if (!await EasyEaApi.IsValidCookie())
        {
            LoggerHelper.Warn("玩家 Cookie 无效，准备跳转登录界面");
            return false;
        }

        LoggerHelper.Info("玩家 Cookie 有效");

        return true;
    }

    /// <summary>
    /// 初始化游戏信息
    /// </summary>
    private async void InitGameInfo()
    {
        Grid_Part1.Visibility = Visibility.Visible;
        Grid_Part2.Visibility = Visibility.Hidden;
        Part2_BtnTools.Visibility = Visibility.Hidden;

        // 关闭服务进程
        CoreUtil.CloseServerProcess();

        DisplayLoadState("正在释放资源服务进程文件...");
        LoggerHelper.Info("正在释放资源服务进程文件...");
        FileHelper.ExtractResFile("Exec.EADesktop.exe", CoreUtil.File_Cache_EADesktop);
        FileHelper.ExtractResFile("Exec.OriginDebug.exe", CoreUtil.File_Cache_OriginDebug);

        /////////////////////////////////////////////////

        DisplayLoadState("正在获取注册表游戏信息...");
        LoggerHelper.Info("正在获取注册表游戏信息...");
        // 从注册表获取游戏安装信息
        foreach (var gameInfo in Base.GameInfoDb)
        {
            LoggerHelper.Info($"开始获取《{gameInfo.Value.Name}》注册表游戏信息...");

            var installDir = RegistryHelper.GetRegistryInstallDir(gameInfo.Value.Regedit);
            if (!string.IsNullOrWhiteSpace(installDir))
            {
                gameInfo.Value.Dir = installDir;
                gameInfo.Value.IsInstalled = true;
                LoggerHelper.Info($"从 Regedit 获取《{gameInfo.Value.Name}》注册表游戏信息成功");
            }
            else
            {
                installDir = RegistryHelper.GetRegistryInstallDir(gameInfo.Value.Regedit2);
                if (!string.IsNullOrWhiteSpace(installDir))
                {
                    gameInfo.Value.Dir = installDir;
                    gameInfo.Value.IsInstalled = true;
                    LoggerHelper.Info($"从 Regedit2 获取《{gameInfo.Value.Name}》注册表游戏信息成功");
                }
            }

            if (!gameInfo.Value.IsInstalled)
                LoggerHelper.Warn($"未获取到《{gameInfo.Value.Name}》注册表游戏信息");
        }

        /////////////////////////////////////////////////

        DisplayLoadState("正在刷新基础 Token 信息...");
        LoggerHelper.Info("正在刷新基础 Token 信息...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("刷新基础 Token 信息失败，程序终止，请检查网络连接");
                LoggerHelper.Error("刷新基础 Token 信息失败，程序终止，请检查网络连接");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState($"刷新基础 Token 信息失败，开始第 {i} 次重试中...");
                LoggerHelper.Warn($"刷新基础 Token 信息失败，开始第 {i} 次重试中...");
            }

            if (await Ready.RefreshBaseTokens())
            {
                LoggerHelper.Info("刷新基础 Token 信息成功");
                break;
            }
        }

        DisplayLoadState("正在获取玩家账号信息...");
        LoggerHelper.Info("正在获取玩家账号信息...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("刷新基础 Token 信息失败，程序终止，请检查网络连接");
                LoggerHelper.Error("刷新基础 Token 信息失败，程序终止，请检查网络连接");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState($"刷新基础 Token 信息失败，开始第 {i} 次重试中...");
                LoggerHelper.Info($"刷新基础 Token 信息失败，开始第 {i} 次重试中...");
            }

            if (await Ready.GetLoginAccountInfo())
            {
                LoggerHelper.Info("获取当前登录玩家信息成功");
                break;
            }
        }

        /////////////////////////////////////////////////

        // 防止新数据丢失
        Globals.Write(true);

        DisplayLoadState("初始化完成，开始启动主程序...");
        LoggerHelper.Info("初始化完成，开始启动主程序...");

        var mainWindow = new MainWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = mainWindow;
        // 关闭初始化窗口
        this.Close();

        // 显示主窗口
        mainWindow.Show();
    }

    /// <summary>
    /// 初始化WebView2登录信息
    /// </summary>
    private async Task InitWebView2()
    {
        LoggerHelper.Info("开始加载 WebView2 登录界面...");

        Grid_Part1.Visibility = Visibility.Hidden;
        Grid_Part2.Visibility = Visibility.Visible;
        Part2_BtnTools.Visibility = Visibility.Visible;

        var options = new CoreWebView2EnvironmentOptions();

        // 初始化WebView2环境
        var env = await CoreWebView2Environment.CreateAsync(null, Globals.GetAccountCacheDir(), options);
        await WebView2_Main.EnsureCoreWebView2Async(env);

        // 禁止Dev开发工具
        WebView2_Main.CoreWebView2.Settings.AreDevToolsEnabled = false;
        // 禁止右键菜单
        WebView2_Main.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        // 禁止浏览器缩放
        WebView2_Main.CoreWebView2.Settings.IsZoomControlEnabled = false;
        // 禁止显示状态栏（鼠标悬浮在链接上时右下角没有url地址显示）
        WebView2_Main.CoreWebView2.Settings.IsStatusBarEnabled = false;

        // 新窗口打开页面的处理
        WebView2_Main.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        // Url变化的处理
        WebView2_Main.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;

        // 导航开始事件
        WebView2_Main.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
        // 导航完成事件
        WebView2_Main.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

        // 用于注销账号
        if (IsLogout)
        {
            WinButton_Clear_Click(null, null);
            return;
        }

        // 导航到指定Url
        WebView2_Main.CoreWebView2.Navigate(_host);
    }

    private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();
        e.NewWindow = WebView2_Main.CoreWebView2;
        deferral.Complete();
    }

    private async void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
    {
        var source = WebView2_Main.Source.ToString();
        LoggerHelper.Info($"当前 WebView2 地址: {source}");
        if (!source.Contains("127.0.0.1/success?code="))
            return;

        LoggerHelper.Info("玩家登录成功，开始获取 cookies...");
        var cookies = await WebView2_Main.CoreWebView2.CookieManager.GetCookiesAsync(null);
        if (cookies == null)
        {
            LoggerHelper.Warn("登录账号成功，获取Cookie失败，请尝试清除WebView2缓存");
            return;
        }

        LoggerHelper.Info("发现 cookies 文件，开始遍历中...");
        foreach (var item in cookies)
        {
            if (item.Name.Equals("remid", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    Account.Remid = item.Value;
                    LoggerHelper.Info($"获取 Remid 成功: {Account.Remid}");
                }
            }
            else if (item.Name.Equals("sid", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    Account.Sid = item.Value;
                    LoggerHelper.Info($"获取 Sid 成功: {Account.Sid}");
                }
            }
        }

        InitGameInfo();
    }

    private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        WebView2_Main.Visibility = Visibility.Hidden;
        WebView2_Loading.Visibility = Visibility.Visible;
    }

    private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        WebView2_Main.Visibility = Visibility.Visible;
        WebView2_Loading.Visibility = Visibility.Hidden;
    }

    /// <summary>
    /// 清空WebView2缓存
    /// </summary>
    private async void WinButton_Clear_Click(object sender, RoutedEventArgs e)
    {
        await WebView2_Main.CoreWebView2.ExecuteScriptAsync("localStorage.clear()");
        WebView2_Main.CoreWebView2.CookieManager.DeleteAllCookies();
        WebView2_Main.CoreWebView2.Navigate(_host);
        LoggerHelper.Info("清空 WebView2 缓存成功");
    }

    /// <summary>
    /// 重新加载登录页面
    /// </summary>
    private void WinButton_Refush_Click(object sender, RoutedEventArgs e)
    {
        WebView2_Main.CoreWebView2.Navigate(_host);
        LoggerHelper.Info("重新加载登录页面成功");
    }
}
