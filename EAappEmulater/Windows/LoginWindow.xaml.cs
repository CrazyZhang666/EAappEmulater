using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Helper;
using Microsoft.Web.WebView2.Core;

namespace EAappEmulater.Windows;

/// <summary>
/// LoginWindow.xaml 的交互逻辑
/// </summary>
public partial class LoginWindow
{
    private const string _host = "https://accounts.ea.com/connect/auth?response_type=code&locale=zh_CN&client_id=EADOTCOM-WEB-SERVER";

    /**
     * 2024/04/29
     * 关于 WebView2 第一次加载设置 Visibility 不可见会导致短暂白屏
     * https://github.com/MicrosoftEdge/WebView2Feedback/issues/3707#issuecomment-1679440957
     */

    /// <summary>
    /// 是否清理缓存（用于切换新账号使用）
    /// </summary>
    private readonly bool _isClear;

    public LoginWindow(bool isClear)
    {
        InitializeComponent();
        this._isClear = isClear;
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private void Window_Login_Loaded(object sender, RoutedEventArgs e)
    {
    }

    /// <summary>
    /// 窗口内容呈现完毕后事件
    /// </summary>
    private async void Window_Login_ContentRendered(object sender, EventArgs e)
    {
        // 初始化WebView2
        await InitWebView2();
    }

    /// <summary>
    /// 窗口关闭时事件
    /// </summary>
    private void Window_Login_Closing(object sender, CancelEventArgs e)
    {
        WebView2_Main?.Dispose();

        ////////////////////////////////

        var accountWindow = new AccountWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = accountWindow;

        // 显示切换账号窗口
        accountWindow.Show();
    }

    /// <summary>
    /// 初始化WebView2登录信息
    /// </summary>
    private async Task InitWebView2()
    {
        try
        {
            LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.InitWebView2"));

            var options = new CoreWebView2EnvironmentOptions();

            // 初始化WebView2环境
            var env = await CoreWebView2Environment.CreateAsync(null, Globals.GetAccountCacheDir(), options);
            await WebView2_Main.EnsureCoreWebView2Async(env);

            LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.InitWebView2Success"));

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

            // 用于更换新账号
            if (_isClear)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.ClearWebView2Cache"));
                await ClearWebView2Cache();
            }
            else
            {
                LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.LoadWebView2LoginView"));

                // 导航到指定Url
                WebView2_Main.CoreWebView2.Navigate(_host);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Windows.LoginWindow.WebView2InitError", ex));
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
        LoggerHelper.Trace("SourceChanged");

        var source = WebView2_Main.Source.ToString();
        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.CurrentWebView2Source", source));
        if (!source.Contains("test.pulse.ea.com"))
            return;

        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.WebView2LoginSuccess"));
        var cookies = await WebView2_Main.CoreWebView2.CookieManager.GetCookiesAsync(null);
        if (cookies == null)
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Windows.LoginWindow.WebView2GetCookieError"));
            return;
        }

        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.FindCookieFile"));
        LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.FindCookieCount", cookies.Count));

        foreach (var item in cookies)
        {
            if (item.Name.Equals("remid", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    IniHelper.WriteString("Cookie", "Remid", item.Value, Globals.GetAccountIniPath());
                    LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.RemidGetSuccess", item.Value));
                    continue;
                }
            }

            if (item.Name.Equals("sid", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    IniHelper.WriteString("Cookie", "Sid", item.Value, Globals.GetAccountIniPath());
                    LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.SidGetSuccess", item.Value));
                    continue;
                }
            }
        }

        ////////////////////////////////

        this.Close();
    }

    private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        WebView2_Main.Visibility = Visibility.Hidden;
        WebView2_Loading.Visibility = Visibility.Visible;

        LoggerHelper.Trace("NavigationStarting");
    }

    private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        WebView2_Main.Visibility = Visibility.Visible;
        WebView2_Loading.Visibility = Visibility.Hidden;

        LoggerHelper.Trace("NavigationCompleted");
    }

    /// <summary>
    /// 清空WebView2缓存
    /// </summary>
    /// <returns></returns>
    private async Task ClearWebView2Cache()
    {
        await WebView2_Main.CoreWebView2.ExecuteScriptAsync("localStorage.clear()");
        WebView2_Main.CoreWebView2.CookieManager.DeleteAllCookies();
        WebView2_Main.CoreWebView2.Navigate(_host);

        LoggerHelper.Info(I18nHelper.I18n._("ClearWebView2CacheSuccess"));
    }

    /// <summary>
    /// 重新加载登录页面
    /// </summary>
    [RelayCommand]
    private void ReloadLoginPage()
    {
        WebView2_Main.CoreWebView2.Navigate(_host);
        LoggerHelper.Info(I18nHelper.I18n._("ReloadWebView2ViewSuccess"));
    }
}
