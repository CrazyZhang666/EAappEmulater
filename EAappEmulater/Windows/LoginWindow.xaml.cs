using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Api;
using EAappEmulater.Core;
using EAappEmulater.Helper;
using Microsoft.Web.WebView2.Core;

namespace EAappEmulater.Windows;

/// <summary>
/// LoginWindow.xaml 的交互逻辑
/// </summary>
public partial class LoginWindow
{
    private string _host = "";

    /**
     * 2024/04/29
     * 关于 WebView2 第一次加载设置 Visibility 不可见会导致短暂白屏
     * https://github.com/MicrosoftEdge/WebView2Feedback/issues/3707#issuecomment-1679440957
     */

    /// <summary>
    /// 是否清理缓存（用于切换新账号使用）
    /// </summary>
    private readonly bool _isClear;

    public LoginWindow(bool isClear, string host = "")
    {
        InitializeComponent();
        this._isClear = isClear;
        if (String.IsNullOrEmpty(host)) { 
            _host = host;
        }
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

            if (String.IsNullOrEmpty(_host))
            {
                var loginUrlData = await Api.EaApi.GetToken();
                if (loginUrlData != null && !loginUrlData.IsSuccess)
                {
                    _host = loginUrlData.Content;
                }
                else
                {
                    _host = "https://accounts.ea.com/connect/auth?response_type=code&locale=zh_CN&client_id=EADOTCOM-WEB-SERVER";
                }
            }
            

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

            // 添加请求拦截器过滤器
            WebView2_Main.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

            await WebView2_Main.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
    window.addEventListener('DOMContentLoaded', () => {
        let href = window.location.href;
        if (href.startsWith('https://pc.ea.com/login.html')) {
            window.chrome.webview.postMessage({ type: 'redirect', url: href });
        }
    });
");

            // 注册事件处理程序
            WebView2_Main.CoreWebView2.WebResourceRequested += (sender, args) =>
            {
                var uri = new Uri(args.Request.Uri);
                if (uri.Host.Equals("pc.ea.com", StringComparison.OrdinalIgnoreCase))
                {
                    // 设置或覆盖请求头
                    args.Request.Headers.SetHeader("x-juno-max-img-res", "1080");
                    args.Request.Headers.SetHeader("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Origin/10.6.0.00000 EAApp/13.377.0.5890 Chrome/109.0.5414.120 Safari/537.36");

                }
            };

            WebView2_Main.CoreWebView2.NewWindowRequested += (sender, args) =>
            {
                args.Handled = true; // 阻止 WebView2 创建新窗口

                var targetUri = args.Uri;

                // 在当前窗口导航目标链接
                WebView2_Main.CoreWebView2.Navigate(targetUri);
            };

            await WebView2_Main.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
    window.open = function(url) {
        location.href = url;
        return null;
    };
    document.querySelectorAll('a[target=""_blank""]').forEach(a => a.target = '_self');
");
            WebView2_Main.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Origin/10.6.0.00000 EAApp/13.377.0.5890 Chrome/109.0.5414.120 Safari/537.36";

            WebView2_Main.CoreWebView2.WebMessageReceived += (s, e) =>
            {
                var msg = e.WebMessageAsJson;

                try
                {
                    var data = System.Text.Json.JsonDocument.Parse(msg);
                    if (data.RootElement.GetProperty("type").GetString() == "redirect")
                    {
                        var redirectUrl = data.RootElement.GetProperty("url").GetString();

                        // 提取 token 逻辑
                        var uri = new Uri(redirectUrl.Replace("qrc:/", "http://fake/")); // 避免非法 URI 报错
                        var fragment = uri.Fragment; // 取 # 后的参数
                        var queryParams = System.Web.HttpUtility.ParseQueryString(fragment.TrimStart('#'));
                        var token = queryParams["access_token"];
                        IniHelper.WriteString("Cookie", "AccessToken", token, Globals.GetAccountIniPath());
                        IniHelper.WriteString("Cookie", "OriginPCToken", token, Globals.GetAccountIniPath());

                        Account.AccessToken = token;
                        Account.OriginPCToken = token;

                        // 跳转空白页
                        WebView2_Main.CoreWebView2.Navigate("about:blank");
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Debug("解析重定向失败：" + ex.Message);
                }
            };



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
        if (!source.Contains("pc.ea.com"))
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
        bool isRemidGet = false;
        foreach (var item in cookies)
        {
            if (!item.Domain.EndsWith(".ea.com", StringComparison.OrdinalIgnoreCase))
                continue;

            if (item.Name.Equals("remid", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    IniHelper.WriteString("Cookie", "Remid", item.Value, Globals.GetAccountIniPath());
                    LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.RemidGetSuccess", item.Value));
                    isRemidGet = true;
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
        if (isRemidGet)
        {
            this.Close();
        }
    }

    

    private void ProcessHeader(string name, string value, ref bool gotRemid)
    {
        if (!name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var cookie in value.Split(';'))
        {
            var kv = cookie.Split('=', 2);
            var key = kv[0].Trim();
            var val = kv.Length > 1 ? kv[1].Trim() : "";

            if (key.Equals("sid", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(val))
            {
                IniHelper.WriteString("Cookie", "Sid", val, Globals.GetAccountIniPath());
                LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.SidGetSuccess", val));
            }
            else if (key.Equals("remid", StringComparison.OrdinalIgnoreCase) &&
                     !string.IsNullOrEmpty(val))
            {
                IniHelper.WriteString("Cookie", "Remid", val, Globals.GetAccountIniPath());
                LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.RemidGetSuccess", val));
                gotRemid = true;
            }
        }
    }

    private void ContinueFetch(string requestId)
    {
        _ = WebView2_Main.CoreWebView2.CallDevToolsProtocolMethodAsync(
            "Fetch.continueRequest",
            $@"{{ ""requestId"": ""{requestId}"" }}");
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
