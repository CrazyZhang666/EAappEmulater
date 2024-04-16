using BF1ModTools.Utils;
using BF1ModTools.Helper;

namespace BF1ModTools;

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
        LoggerHelper.Info($"欢迎使用 {AppName} 程序");

        AppMainMutex = new Mutex(true, AppName, out var createdNew);
        if (!createdNew)
        {
            LoggerHelper.Warn("请不要重复打开，程序已经运行");
            MsgBoxHelper.Warning($"请不要重复打开，程序已经运行\n如果一直提示，请到\"任务管理器-详细信息（win7为进程）\"里\n强制结束 \"{AppName}.exe\" 程序");
            Current.Shutdown();
            return;
        }

        //////////////////////////////////////////////////////////////////////////////////

        LoggerHelper.Info("正在进行 .NET 6.0 版本检测中...");
        if (Environment.Version < new Version("6.0.29"))
        {
            if (MessageBox.Show("发现 .NET 6.0 版本过低，请前往微软官网下载更新版本\nhttps://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-6.0.29-windows-x64-installer",
                ".NET 6.0 版本检测", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                ProcessHelper.OpenLink("https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-6.0.29-windows-x64-installer");
                Current.Shutdown();
                return;
            }
        }
        LoggerHelper.Info($"当前系统 .NET 6.0 版本为 {Environment.Version}");

        LoggerHelper.Info("正在进行 WebVieww2 环境检测中...");
        if (!CoreUtil.CheckWebView2Env())
        {
            if (MessageBox.Show("未发现 WebView2 运行环境，请前往微软官网下载安装\nhttps://go.microsoft.com/fwlink/p/?LinkId=2124703",
                "WebView2 环境检测", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                ProcessHelper.OpenLink("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                Current.Shutdown();
                return;
            }
        }
        LoggerHelper.Info($"当前系统 WebVieww2 环境正常");

        LoggerHelper.Info("正在进行工具箱文件路径检测中...");
        var currentDir = Directory.GetCurrentDirectory();
        if (CoreUtil.HasChinesePath(currentDir))
        {
            MsgBoxHelper.Warning($"检测到工具箱运行路径含有中文，请在英文目录下运行\n{currentDir}");
            LoggerHelper.Warn($"检测到工具箱运行路径含有中文，请在英文目录下运行 {currentDir}");
            Current.Shutdown();
            return;
        }
        LoggerHelper.Info($"工具箱运行路径正常 {currentDir}");

        LoggerHelper.Info("正在进行工具箱数据目录 AppData 完整性检测中...");
        if (!CoreUtil.IsFullAppData())
        {
            MsgBoxHelper.Warning("检测到工具箱数据目录 AppData 已损坏，请重新解压文件");
            LoggerHelper.Warn("检测到工具箱数据目录 AppData 已损坏，请重新解压文件");
            Current.Shutdown();
            return;
        }
        LoggerHelper.Info("工具箱数据目录 AppData 完整");

        LoggerHelper.Info("正在进行工具箱管理员权限检测中...");
        if (!CoreUtil.IsRunAsAdmin())
        {
            MsgBoxHelper.Warning("检测到工具箱未拥有管理员权限，请尝试右键管理员运行");
            LoggerHelper.Warn("检测到工具箱未拥有管理员权限，请尝试右键管理员运行");
            Current.Shutdown();
            return;
        }
        LoggerHelper.Info("工具箱管理员正常");

        //////////////////////////////////////////////////////////////////////////////////

        // 注册异常捕获
        RegisterEvents();
        // 注册编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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
        CoreUtil.SaveCrashLog(msg);
    }

    /// <summary>
    /// 非UI线程未捕获异常处理事件（例如自己创建的一个子线程）
    /// </summary>
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var msg = GetExceptionInfo(e.ExceptionObject as Exception, e.ToString());
        CoreUtil.SaveCrashLog(msg);
    }

    /// <summary>
    /// Task线程内未捕获异常处理事件
    /// </summary>
    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        var msg = GetExceptionInfo(e.Exception, e.ToString());
        CoreUtil.SaveCrashLog(msg);
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
}
