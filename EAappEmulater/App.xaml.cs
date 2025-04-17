using EAappEmulater.Helper;
using EAappEmulater.Utils;

namespace EAappEmulater;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 主程序互斥体
    /// </summary>
    public static Mutex AppMainMutex;
    /// <summary>
    /// 应用程序名称
    /// </summary>
    private readonly string AppName = ResourceAssembly.GetName().Name;

    /// <summary>
    /// 保证程序只能同时启动一个
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {

        if (!string.IsNullOrWhiteSpace(Globals.DefaultLanguage))
        {
            SetLanguage(Globals.DefaultLanguage);
        }
        else
        {
            var systemLanguage = CultureInfo.CurrentUICulture.Name;
            if (systemLanguage.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                SetLanguage("zh-CN");
            }
            else
            {
                SetLanguage("en-US");
            }
        }

        LoggerHelper.Info(I18nHelper.I18n._("App.Welcome", AppName));

        // 注册异常捕获
        RegisterEvents();
        // 注册编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        //////////////////////////////////////////////////////

        AppMainMutex = new Mutex(true, AppName, out var createdNew);
        if (!createdNew)
        {
            LoggerHelper.Warn(I18nHelper.I18n._("App.DuplicateWarn"));
            MsgBoxHelper.Warning(I18nHelper.I18n._("App.DuplicateWarn"));
            Current.Shutdown();
            return;
        }

        //////////////////////////////////////////////////////

        LoggerHelper.Info(I18nHelper.I18n._("App.WebView2EnvCheck"));
        if (!CoreUtil.CheckWebView2Env())
        {
            if (MessageBox.Show(I18nHelper.I18n._("App.WebView2EnvCheckNotFound"),
                "WebView2 Warn", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                ProcessHelper.OpenLink("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                Current.Shutdown();
                return;
            }
        }
        LoggerHelper.Info(I18nHelper.I18n._("App.WebView2EnvCheckSuccess"));

        LoggerHelper.Info(I18nHelper.I18n._("App.TCPPortCheck"));
        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        var ipEndPoints = ipProperties.GetActiveTcpListeners();
        foreach (var endPoint in ipEndPoints)
        {
            if (endPoint.Port == 3216)
            {
                LoggerHelper.Error(I18nHelper.I18n._("App.TCPPortCheck3216"));
                MsgBoxHelper.Error(I18nHelper.I18n._("App.TCPPortCheck3216"), I18nHelper.I18n._("App.TCPPortCheckErrorTitle"));
                Current.Shutdown();
                return;
            }

            if (endPoint.Port == 3215)
            {
                LoggerHelper.Error(I18nHelper.I18n._("App.TCPPortCheck3215"));
                MsgBoxHelper.Error(I18nHelper.I18n._("App.TCPPortCheck3215"), I18nHelper.I18n._("App.TCPPortCheckErrorTitle"));
                Current.Shutdown();
                return;
            }

            if (endPoint.Port == 4219)
            {
                LoggerHelper.Error(I18nHelper.I18n._("App.TCPPortCheck4219"));
                MsgBoxHelper.Error(I18nHelper.I18n._("App.TCPPortCheck4219"), I18nHelper.I18n._("App.TCPPortCheckErrorTitle"));
                Current.Shutdown();
                return;
            }
        }
        Process currentProcess = Process.GetCurrentProcess();
        if (currentProcess.ProcessName != "EADesktop")
        {
            LoggerHelper.Error(I18nHelper.I18n._("App.ErrorFileName"));
            MsgBoxHelper.Error(I18nHelper.I18n._("App.ErrorFileName"));
            Current.Shutdown();
            return;
        }
        LoggerHelper.Info(I18nHelper.I18n._("App.TCPPortCheckSuccess"));

        //////////////////////////////////////////////////////

        base.OnStartup(e);
    }

    /// <summary>
    /// 注册异常捕获事件
    /// </summary>
    private void RegisterEvents()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// UI线程未捕获异常处理事件（UI主线程）
    /// </summary>
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var msg = GetExceptionInfo(e.Exception, e.ToString());
        SaveCrashLog(msg);
    }

    /// <summary>
    /// 非UI线程未捕获异常处理事件（例如自己创建的一个子线程）
    /// </summary>
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var msg = GetExceptionInfo(e.ExceptionObject as Exception, e.ToString());
        SaveCrashLog(msg);
    }

    /// <summary>
    /// Task线程内未捕获异常处理事件
    /// </summary>
    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        // 2024/07/25
        // 目前无法解决这个异常，所以停止生成对应崩溃日志
        if (e.Exception.Message.Equals("A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (由于线程退出或应用程序请求，已中止 I/O 操作。)"))
        {
            LoggerHelper.Error(I18nHelper.I18n._("App.TaskEx", e.Exception));
            return;
        }

        var msg = GetExceptionInfo(e.Exception, e.ToString());
        SaveCrashLog(msg);
    }

    /// <summary>
    /// 生成自定义异常消息
    /// </summary>
    private string GetExceptionInfo(Exception ex, string backStr)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"程序版本: {CoreUtil.VersionInfo}");
        builder.AppendLine($"用户名称: {CoreUtil.UserName}");
        builder.AppendLine($"电脑名称: {CoreUtil.MachineName}");
        builder.AppendLine($"系统版本: {CoreUtil.OSVersion}");
        builder.AppendLine($"系统目录: {CoreUtil.SystemDirectory}");
        builder.AppendLine($"运行库平台: {CoreUtil.RuntimeVersion}");
        builder.AppendLine($"运行库版本: {CoreUtil.OSArchitecture}");
        builder.AppendLine($"运行库环境: {CoreUtil.RuntimeIdentifier}");
        builder.AppendLine("------------------------------");
        builder.AppendLine($"崩溃时间: {DateTime.Now}");

        if (ex is not null)
        {
            builder.AppendLine($"异常类型: {ex.GetType().Name}");
            builder.AppendLine($"异常信息: {ex.Message}");
            builder.AppendLine($"堆栈调用: \n{ex.StackTrace}");
        }
        else
        {
            builder.AppendLine($"未处理异常: {backStr}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// 保存崩溃日志
    /// </summary>
    private void SaveCrashLog(string log)
    {
        try
        {
            var path = Path.Combine(CoreUtil.Dir_Log_Crash, $"CrashReport-{DateTime.Now:yyyyMMdd_HHmmss_ffff}.log");
            File.WriteAllText(path, log);
        }
        catch { }
    }

    public static void SetLanguage(string lang)
    {
        string dictPath = $"Assets/Files/Lang/{lang}.xaml";
        var dict = new ResourceDictionary() { Source = new Uri(dictPath, UriKind.Relative) };

        // 清理旧的语言资源
        var oldDict = Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("Assets/Files/Lang"));

        if (oldDict != null)
            Current.Resources.MergedDictionaries.Remove(oldDict);

        Current.Resources.MergedDictionaries.Add(dict);
        Globals.Language = lang;
    }
}