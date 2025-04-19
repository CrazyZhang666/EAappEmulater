using EAappEmulater.Core;
using EAappEmulater.Helper;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;

namespace EAappEmulater.Views
{
    public partial class EADesktopWeb : UserControl
    {
        private bool isWebViewInitialized = false;

        public EADesktopWeb()
        {
            InitializeComponent();
            this.Loaded += EADesktopWeb_Loaded;
        }

        private async void EADesktopWeb_Loaded(object sender, RoutedEventArgs e)
        {
            //防止多次初始化
            if (isWebViewInitialized)
            {
                return;
            }
            isWebViewInitialized = true;

            var options = new CoreWebView2EnvironmentOptions();
            var env = await CoreWebView2Environment.CreateAsync(null, Globals.GetAccountCacheDir(), options);
            await webView2.EnsureCoreWebView2Async(env);
            //0.8缩放才能显示登录按钮
            webView2.ZoomFactor = 0.8;
            webView2.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            webView2.CoreWebView2.Settings.UserAgent = "";
            webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView2.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            webView2.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            webView2.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            var cookieManager = webView2.CoreWebView2.CookieManager;
            var cookie = cookieManager.CreateCookie(
                name: "remid",
                value: Account.Remid,
                Domain: ".ea.com",
                Path: "/connect");

            cookie.IsHttpOnly = true;
            cookie.IsSecure = true;
            cookieManager.AddOrUpdateCookie(cookie);
            var cookie2 = cookieManager.CreateCookie(
                name: "sid",
                value: Account.Sid,
                Domain: ".ea.com",
                Path: "/connect");

            cookie2.IsHttpOnly = true;
            cookie2.IsSecure = true;
            cookieManager.AddOrUpdateCookie(cookie2);
            webView2.CoreWebView2.Navigate($"https://pc.ea.com/login.html#access_token={Account.AccessToken}&token_type=Bearer");
        }

        private  void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();
            e.Handled = true;
            webView2.CoreWebView2.Navigate(e.Uri.ToString());
            deferral.Complete();
        }

        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            string requestUri = e.Request.Uri.ToString();
            if (requestUri.Contains("pc.ea.com"))
            {
                e.Request.Headers.SetHeader("x-juno-max-img-res", "1080");
                e.Request.Headers.SetHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Origin/10.6.0.00000 EAApp/13.377.0.5890 Chrome/109.0.5414.120 Safari/537.36");
            }
        }
        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                string currentUrl = webView2.CoreWebView2.Source;

                if (currentUrl.Contains("login.html#access_token") || currentUrl.Contains("logout.html"))
                {
                    webView2.CoreWebView2.Navigate("https://pc.ea.com/zh-hans");
                }
            }
        }
    }
}