using EAappEmulater.Core;
using EAappEmulater.Helper;
using Microsoft.Web.WebView2.Core;

namespace EAappEmulater;

/// <summary>
/// LoginWindow.xaml 的交互逻辑
/// </summary>
public partial class LoginWindow
{
    private const string _host = "https://accounts.ea.com/connect/auth?client_id=sparta-backend-as-user-pc&response_type=code&release_type=none";

    /// <summary>
    /// 是否登出当前账号（用于切换新账号使用）
    /// </summary>
    public bool IsLogout { get; set; } = false;

    public LoginWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private async void Window_Login_Loaded(object sender, RoutedEventArgs e)
    {
        await InitWebView2();
    }

    /// <summary>
    /// 窗口关闭时事件
    /// </summary>
    private void Window_Login_Closing(object sender, CancelEventArgs e)
    {
        WebView2_Main?.Dispose();
    }

    /// <summary>
    /// 初始化WebView2登录信息
    /// </summary>
    private async Task InitWebView2()
    {
        LoggerHelper.Info("开始加载 WebView2 登录界面...");

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
            LoggerHelper.Info("开始注销当前登录账号...");
            WinButton_Clear_Click(null, null);
        }
        else
        {
            // 导航到指定Url
            WebView2_Main.CoreWebView2.Navigate(_host);
        }
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
        LoggerHelper.Info($"Cookie 数量为 {cookies.Count}");

        foreach (var item in cookies)
        {
            if (item.Name.Equals("remid", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    Account.Remid = item.Value;
                    LoggerHelper.Info($"获取 Remid 成功: {Account.Remid}");
                    continue;
                }
            }

            if (item.Name.Equals("sid", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    Account.Sid = item.Value;
                    LoggerHelper.Info($"获取 Sid 成功: {Account.Sid}");
                    continue;
                }
            }
        }

        ////////////////////////////////

        // 保存新数据，防止丢失
        Globals.Write(true);

        var loadWindow = new LoadWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = loadWindow;
        // 关闭登录窗口
        this.Close();

        // 显示加载窗口
        loadWindow.Show();
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
