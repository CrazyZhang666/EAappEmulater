﻿using EAappEmulater.Helper;
using EAappEmulater.Utils;
using EAappEmulater.Windows;

namespace EAappEmulater;

using EAappEmulater.Enums;

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
        // Load global config first so we know if user specified a language
        Globals.Read();

        // Determine language to set following rules:
        // - If Config (Globals.DefaultLanguage) has a supported language -> use it
        // - If Config has but not supported -> use system default (if supported) else zh-CN
        // - If no Config -> use system default (if supported) else zh-CN
        string langToSet = string.Empty;
        var supported = LanguageConfigHelper.GetLanguages().Select(x => x.Code).ToList();

        if (!string.IsNullOrWhiteSpace(Globals.DefaultLanguage) && supported.Any(s => s.Equals(Globals.DefaultLanguage, StringComparison.OrdinalIgnoreCase)))
        {
            langToSet = Globals.DefaultLanguage;
        }
        else
        {
            var systemLanguage = CultureInfo.CurrentUICulture.Name;
            if (!string.IsNullOrWhiteSpace(systemLanguage))
            {
                // exact match
                var exact = supported.FirstOrDefault(s => s.Equals(systemLanguage, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(exact))
                {
                    langToSet = exact;
                }
                else
                {
                    // match by language part (e.g., "en" -> "en-US")
                    var langPart = systemLanguage.Split(new[] { '-', '_' })[0];
                    var partial = supported.FirstOrDefault(s => s.StartsWith(langPart, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(partial))
                        langToSet = partial;
                }
            }

            // if config had value but not supported, we already tried system; if still empty continue
            if (string.IsNullOrWhiteSpace(langToSet) && !string.IsNullOrWhiteSpace(Globals.DefaultLanguage))
            {
                // config invalid and system didn't match -> fallback to zh-CN
                langToSet = "zh-CN";
            }

            // if no config and system not supported -> fallback to zh-CN
            if (string.IsNullOrWhiteSpace(langToSet) && string.IsNullOrWhiteSpace(Globals.DefaultLanguage))
            {
                langToSet = "zh-CN";
            }
        }

        // Finally set language
        SetLanguage(langToSet);

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
#if !DEBUG
using (Process currentProcess = Process.GetCurrentProcess())
{
    if (currentProcess.ProcessName != "EADesktop")
    {
        LoggerHelper.Error(I18nHelper.I18n._("App.ErrorFileName"));
        MsgBoxHelper.Error(I18nHelper.I18n._("App.ErrorFileName"));
        Current.Shutdown();
        return;
    }
}
#endif
        LoggerHelper.Info(I18nHelper.I18n._("App.TCPPortCheckSuccess"));

        //////////////////////////////////////////////////////

        // 解析命令行参数
        ParseCommandLine(e);

        // 检查自动登录
        CheckAutoLogin();

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

    /// <summary>
    /// 解析命令行参数
    /// </summary>
    private void ParseCommandLine(StartupEventArgs e)
    {
        try
        {
            // e.Args 不包含程序路径，只包含实际的命令行参数
            var args = e.Args;
            if (args == null || args.Length == 0)
                return;

            LoggerHelper.Info($"命令行参数: {string.Join(" ", args)}");
            LoggerHelper.Info($"参数数量: {args.Length}");

            // 遍历参数，处理 --account 和 --game
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                // 处理 --account
                if (arg.Equals("--account", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("--account="))
                {
                    string slotValue = null;

                    // 格式: --account=S0
                    if (arg.StartsWith("--account="))
                    {
                        slotValue = arg.Substring("--account=".Length);
                    }
                    // 格式: --account S0
                    else if (i + 1 < args.Length)
                    {
                        slotValue = args[i + 1];
                        i++; // 跳过下一个参数
                    }

                    // 解析账号槽
                    if (!string.IsNullOrEmpty(slotValue))
                    {
                        // 尝试直接解析 S0 格式
                        if (Enum.TryParse(slotValue, out AccountSlot slot))
                        {
                            Globals.CommandLineAccountSlot = slot;
                            LoggerHelper.Info($"命令行指定账号槽: {slot}");
                            continue;
                        }
                        // 尝试解析数字格式 0, 1, 2...
                        else if (int.TryParse(slotValue, out int slotNumber) && slotNumber >= 0 && slotNumber <= 99)
                        {
                            var slotName = $"S{slotNumber}";
                            if (Enum.TryParse(slotName, out AccountSlot slot2))
                            {
                                Globals.CommandLineAccountSlot = slot2;
                                LoggerHelper.Info($"命令行指定账号槽: {slotName}");
                                continue;
                            }
                        }
                    }
                }

                // 处理 --game
                else if (arg.Equals("--game", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("--game="))
                {
                    string gameValue = null;

                    // 格式: --game=BF4
                    if (arg.StartsWith("--game="))
                    {
                        gameValue = arg.Substring("--game=".Length);
                    }
                    // 格式: --game BF4
                    else if (i + 1 < args.Length)
                    {
                        gameValue = args[i + 1];
                        i++; // 跳过下一个参数
                    }

                    // 解析游戏类型
                    if (!string.IsNullOrEmpty(gameValue))
                    {
                        if (Enum.TryParse(gameValue, true, out GameType gameType))
                        {
                            Globals.CommandLineGameType = gameType;
                            LoggerHelper.Info($"命令行指定游戏: {gameType}");
                        }
                        else
                        {
                            LoggerHelper.Warn($"命令行游戏参数无效: {gameValue}");
                        }
                    }
                }
            }

            LoggerHelper.Info($"命令行解析完成 - AccountSlot: {Globals.CommandLineAccountSlot}, GameType: {Globals.CommandLineGameType}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"解析命令行参数失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查并执行自动登录
    /// </summary>
    private void CheckAutoLogin()
    {
        try
        {
            LoggerHelper.Info(I18nHelper.I18n._("App.CheckAutoLogin"));

            // 如果命令行指定了账号槽，使用命令行指定的槽
            if (Globals.CommandLineAccountSlot.HasValue)
            {
                Globals.AccountSlot = Globals.CommandLineAccountSlot.Value;
                LoggerHelper.Info($"使用命令行指定的账号槽: {Globals.AccountSlot}");
            }
            else
            {
                // 检查自动登录是否启用
                if (!Globals.AutoLoginEnabled)
                {
                    LoggerHelper.Info(I18nHelper.I18n._("App.AutoLoginDisabled"));
                    ShowAccountWindow();
                    return;
                }

                LoggerHelper.Info(I18nHelper.I18n._("App.AutoLoginEnabled"));
            }

            // 检查当前选中的账号是否有有效的Cookie
            var accountIniPath = Globals.GetAccountIniPath();
            var remid = IniHelper.ReadString("Cookie", "Remid", accountIniPath);
            var sid = IniHelper.ReadString("Cookie", "Sid", accountIniPath);

            if (string.IsNullOrWhiteSpace(remid) || string.IsNullOrWhiteSpace(sid))
            {
                LoggerHelper.Warn(I18nHelper.I18n._("App.AutoLoginCookieInvalid"));
                ShowAccountWindow();
                return;
            }

            LoggerHelper.Info(I18nHelper.I18n._("App.AutoLoginCookieValid"));

            // 直接启动加载窗口，跳过账号选择
            var loadWindow = new LoadWindow();
            Current.MainWindow = loadWindow;
            loadWindow.Show();
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("App.AutoLoginCheckFailed", ex.Message));
            ShowAccountWindow();
        }
    }

    /// <summary>
    /// 显示账号选择窗口
    /// </summary>
    private void ShowAccountWindow()
    {
        var accountWindow = new AccountWindow();
        Current.MainWindow = accountWindow;
        accountWindow.Show();
    }
}