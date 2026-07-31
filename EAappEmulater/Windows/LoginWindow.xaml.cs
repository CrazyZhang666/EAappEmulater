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
    private readonly bool _isClear;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private JunoOAuthSession _oauthSession;
    private int _callbackHandled;
    private bool _initialized;
    private bool _isClosing;

    public LoginWindow(bool isClear, string host = "")
    {
        InitializeComponent();
        _isClear = isClear;
        _ = host;
    }

    private void Window_Login_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private async void Window_Login_ContentRendered(object sender, EventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await InitWebView2();
    }

    private void Window_Login_Closing(object sender, CancelEventArgs e)
    {
        _isClosing = true;
        _lifetimeCts.Cancel();

        var core = WebView2_Main?.CoreWebView2;
        if (core != null)
        {
            core.NewWindowRequested -= CoreWebView2_NewWindowRequested;
            core.NavigationStarting -= CoreWebView2_NavigationStarting;
            core.NavigationCompleted -= CoreWebView2_NavigationCompleted;
            core.LaunchingExternalUriScheme -= CoreWebView2_LaunchingExternalUriScheme;
        }

        _oauthSession?.Dispose();
        _oauthSession = null;
        WebView2_Main?.Dispose();

        var accountWindow = new AccountWindow();
        Application.Current.MainWindow = accountWindow;
        accountWindow.Show();
    }

    private async Task InitWebView2()
    {
        try
        {
            LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.InitWebView2"));

            var options = new CoreWebView2EnvironmentOptions();
            var environment = await CoreWebView2Environment.CreateAsync(null, Globals.GetAccountCacheDir(), options);
            await WebView2_Main.EnsureCoreWebView2Async(environment);

            var core = WebView2_Main.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.NewWindowRequested += CoreWebView2_NewWindowRequested;
            core.NavigationStarting += CoreWebView2_NavigationStarting;
            core.NavigationCompleted += CoreWebView2_NavigationCompleted;
            core.LaunchingExternalUriScheme += CoreWebView2_LaunchingExternalUriScheme;

            if (_isClear)
            {
                await ClearWebView2Cache();
            }

            await CreateAndNavigateSessionAsync(false);
            LoggerHelper.Info(I18nHelper.I18n._("Windows.LoginWindow.InitWebView2Success"));
        }
        catch (OperationCanceledException) when (_isClosing)
        {
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Windows.LoginWindow.WebView2InitError", ex));
            ShowBrowser();
        }
    }

    private async Task CreateAndNavigateSessionAsync(bool resetCallback)
    {
        ShowLoading();
        var newSession = await EaApi.GetToken(_lifetimeCts.Token);

        if (newSession == null || !IsJunoAuthorizationUrl(newSession.AuthorizationUrl))
        {
            newSession?.Dispose();
            throw new InvalidOperationException("EaApi did not create a valid JUNO login session.");
        }

        _oauthSession?.Dispose();
        _oauthSession = newSession;

        if (resetCallback)
        {
            Interlocked.Exchange(ref _callbackHandled, 0);
        }

        WebView2_Main.CoreWebView2.Navigate(_oauthSession.AuthorizationUrl);
    }

    private static bool IsJunoAuthorizationUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || !uri.Host.Equals("accounts.ea.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["client_id"] == "JUNO_PC_CLIENT" && query["response_type"] == "code" && query["code_challenge_method"] == "S256" && !string.IsNullOrWhiteSpace(query["pc_sign"]) && !string.IsNullOrWhiteSpace(query["code_challenge"]);
    }

    private static bool IsQrcUri(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme.Equals("qrc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJunoCallbackUri(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme.Equals("qrc", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(uri.Host) && uri.AbsolutePath.Equals("/html/login_successful.html", StringComparison.OrdinalIgnoreCase);
    }

    private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (IsQrcUri(e.Uri))
        {
            e.Cancel = true;
            if (IsJunoCallbackUri(e.Uri))
            {
                StartQrcExchange(e.Uri);
            }

            return;
        }

        ShowLoading();
        LoggerHelper.Trace("NavigationStarting");
    }

    private void CoreWebView2_LaunchingExternalUriScheme(object sender, CoreWebView2LaunchingExternalUriSchemeEventArgs e)
    {
        if (!IsQrcUri(e.Uri))
        {
            return;
        }

        e.Cancel = true;
        if (IsJunoCallbackUri(e.Uri))
        {
            StartQrcExchange(e.Uri);
        }
    }

    private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;

        if (IsQrcUri(e.Uri))
        {
            if (IsJunoCallbackUri(e.Uri))
            {
                StartQrcExchange(e.Uri);
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(e.Uri))
        {
            WebView2_Main.CoreWebView2.Navigate(e.Uri);
        }
    }

    private void StartQrcExchange(string callbackUri)
    {
        if (_isClosing || Interlocked.CompareExchange(ref _callbackHandled, 1, 0) != 0)
        {
            return;
        }

        ShowLoading();
        _ = CompleteJunoLoginAsync(callbackUri);
    }

    private async Task CompleteJunoLoginAsync(string callbackUri)
    {
        try
        {
            await SaveEaCookiesAsync();
            var result = await EaApi.GetToken(_oauthSession, callbackUri, _lifetimeCts.Token);

            if (result == null || !result.IsSuccess)
            {
                throw new InvalidOperationException(result?.Exception ?? "EA JUNO token exchange failed.");
            }

            if (!_isClosing)
            {
                Close();
            }
        }
        catch (OperationCanceledException) when (_isClosing)
        {
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"EA JUNO login failed: {ex.Message}");
            _oauthSession?.Dispose();
            _oauthSession = null;

            if (!_isClosing)
            {
                ShowBrowser();
            }
        }
    }

    private async Task SaveEaCookiesAsync()
    {
        string remid = null;
        string sid = null;
        var cookieUris = new[] { "https://accounts.ea.com/", "https://signin.ea.com/", "https://www.ea.com/", null };

        for (var attempt = 0; attempt < 10 && (string.IsNullOrWhiteSpace(remid) || string.IsNullOrWhiteSpace(sid)); attempt++)
        {
            foreach (var cookieUri in cookieUris)
            {
                var foundCookies = await FindEaCookiesAsync(cookieUri);
                remid ??= foundCookies.Remid;
                sid ??= foundCookies.Sid;

                if (!string.IsNullOrWhiteSpace(remid) && !string.IsNullOrWhiteSpace(sid))
                {
                    break;
                }
            }

            if ((string.IsNullOrWhiteSpace(remid) || string.IsNullOrWhiteSpace(sid)) && attempt < 9)
            {
                await Task.Delay(200, _lifetimeCts.Token);
            }
        }

        if (!string.IsNullOrWhiteSpace(remid))
        {
            Account.Remid = remid;
            IniHelper.WriteString("Cookie", "Remid", remid, Globals.GetAccountIniPath());
        }

        if (!string.IsNullOrWhiteSpace(sid))
        {
            Account.Sid = sid;
            IniHelper.WriteString("Cookie", "Sid", sid, Globals.GetAccountIniPath());
        }

        LoggerHelper.Info($"EA login cookie scan completed. RemidFound={!string.IsNullOrWhiteSpace(remid)}, SidFound={!string.IsNullOrWhiteSpace(sid)}.");
    }

    private async Task<(string Remid, string Sid)> FindEaCookiesAsync(string uri)
    {
        try
        {
            var cookies = await WebView2_Main.CoreWebView2.CookieManager.GetCookiesAsync(uri);
            string remid = null;
            string sid = null;

            foreach (var cookie in cookies)
            {
                var domain = cookie.Domain?.TrimStart('.');
                if (string.IsNullOrWhiteSpace(domain) || !domain.Equals("ea.com", StringComparison.OrdinalIgnoreCase) && !domain.EndsWith(".ea.com", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (cookie.Name.Equals("remid", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(cookie.Value))
                {
                    remid ??= cookie.Value;
                }
                else if (cookie.Name.Equals("sid", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(cookie.Value))
                {
                    sid ??= cookie.Value;
                }
            }

            return (remid, sid);
        }
        catch (Exception ex)
        {
            LoggerHelper.Warn($"EA CookieManager query failed: {ex.GetType().Name}.");
            return (null, null);
        }
    }

    private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (Volatile.Read(ref _callbackHandled) == 0)
        {
            ShowBrowser();
        }

        LoggerHelper.Trace("NavigationCompleted");
    }

    private void ShowLoading()
    {
        WebView2_Main.Visibility = Visibility.Hidden;
        WebView2_Loading.Visibility = Visibility.Visible;
    }

    private void ShowBrowser()
    {
        WebView2_Main.Visibility = Visibility.Visible;
        WebView2_Loading.Visibility = Visibility.Hidden;
    }

    private async Task ClearWebView2Cache()
    {
        await WebView2_Main.CoreWebView2.ExecuteScriptAsync("localStorage.clear()");
        WebView2_Main.CoreWebView2.CookieManager.DeleteAllCookies();
        LoggerHelper.Info(I18nHelper.I18n._("ClearWebView2CacheSuccess"));
    }

    [RelayCommand]
    private async Task ReloadLoginPage()
    {
        if (_isClosing || WebView2_Main?.CoreWebView2 == null)
        {
            return;
        }

        if (_oauthSession == null || Volatile.Read(ref _callbackHandled) != 0)
        {
            await CreateAndNavigateSessionAsync(true);
        }
        else
        {
            ShowLoading();
            WebView2_Main.CoreWebView2.Navigate(_oauthSession.AuthorizationUrl);
        }

        LoggerHelper.Info(I18nHelper.I18n._("ReloadWebView2ViewSuccess"));
    }
}
