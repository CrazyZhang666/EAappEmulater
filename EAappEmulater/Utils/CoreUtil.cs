using EAappEmulater.Enums;
using EAappEmulater.Helper;
using Microsoft.Web.WebView2.Core;
using System;

namespace EAappEmulater.Utils;

public static class CoreUtil
{
    #region 配置目录
    public static string Dir_MyDocuments { get; private set; }
    public static string Dir_Default { get; private set; }

    public static string Dir_Cache { get; private set; }
    public static string Dir_Config { get; private set; }
    public static string Dir_Account { get; private set; }
    public static string Dir_Avatar { get; private set; }
    public static string Dir_Service { get; private set; }
    public static string Dir_Log { get; private set; }

    public static string Dir_Log_Crash { get; private set; }
    public static string Dir_Log_NLog { get; private set; }

    public static string File_Service_OriginDebug { get; private set; }
    #endregion

    public static Dictionary<AccountSlot, string> AccountCacheDb { get; private set; } = new();

    public static readonly Version VersionInfo;

    public static readonly string UserName;             // Win10
    public static readonly string MachineName;          // CRAZYZHANG
    public static readonly string OSVersion;            // Microsoft Windows NT 10.0.19045.0
    public static readonly string SystemDirectory;      // C:\Windows\system32

    public static readonly string RuntimeVersion;       // .NET 6.0.29
    public static readonly string OSArchitecture;       // X64
    public static readonly string RuntimeIdentifier;    // win10-x64

    static CoreUtil()
    {
        #region 配置目录
        Dir_MyDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        Dir_Default = Path.Combine(Dir_MyDocuments, "EAappEmulater");

        Dir_Cache = Path.Combine(Dir_Default, "Cache");
        Dir_Config = Path.Combine(Dir_Default, "Config");
        Dir_Account = Path.Combine(Dir_Default, "Account");
        Dir_Avatar = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache");
        Dir_Service = Path.Combine(Dir_Default, "Service");
        Dir_Log = Path.Combine(Dir_Default, "Log");

        Dir_Log_Crash = Path.Combine(Dir_Log, "Crash");
        Dir_Log_NLog = Path.Combine(Dir_Log, "NLog");

        FileHelper.CreateDirectory(Dir_Default);

        FileHelper.CreateDirectory(Dir_Cache);
        FileHelper.CreateDirectory(Dir_Config);
        FileHelper.CreateDirectory(Dir_Account);
        FileHelper.CreateDirectory(Dir_Avatar);
        FileHelper.CreateDirectory(Dir_Service);
        FileHelper.CreateDirectory(Dir_Log);

        FileHelper.CreateDirectory(Dir_Log_Crash);
        FileHelper.CreateDirectory(Dir_Log_NLog);

        File_Service_OriginDebug = Path.Combine(Dir_Service, "OriginDebug.exe");
        #endregion

        // 批量创建账号槽缓存目录
        foreach (int value in Enum.GetValues(typeof(AccountSlot)))
        {
            var path = Path.Combine(Dir_Cache, $"Account{value}");
            FileHelper.CreateDirectory(path);

            AccountCacheDb[(AccountSlot)value] = path;
        }

        VersionInfo = Application.ResourceAssembly.GetName().Version;

        UserName = Environment.UserName;
        MachineName = Environment.MachineName;
        OSVersion = Environment.OSVersion.ToString();
        SystemDirectory = Environment.SystemDirectory;

        RuntimeVersion = RuntimeInformation.FrameworkDescription;
        OSArchitecture = RuntimeInformation.OSArchitecture.ToString();
        RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier;
    }

    /// <summary>
    /// 关闭服务进程
    /// </summary>
    public static void CloseServiceProcess()
    {
        LoggerHelper.Info(I18nHelper.I18n._("Utils.CoreUtil.CloseServiceProcess"));
        ProcessHelper.CloseProcess("OriginDebug");
        ProcessHelper.CloseProcess("Origin");
        ProcessHelper.CloseProcess("BF1ModTools");
    }

    /// <summary>
    /// 检查WebView2依赖
    /// </summary>
    public static bool CheckWebView2Env()
    {
        try
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            LoggerHelper.Info(I18nHelper.I18n._("Utils.CoreUtil.CheckWebView2EnvInfo", version));
            return !string.IsNullOrWhiteSpace(version);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Utils.CoreUtil.CheckWebView2EnvError", ex));
            return false;
        }
    }

    /// <summary>
    /// 判断是否管理员权限运行
    /// </summary>
    public static bool IsRunAsAdmin()
    {
        var current = WindowsIdentity.GetCurrent();
        var windowsPrincipal = new WindowsPrincipal(current);
        return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// 时间戳转本地时间
    /// </summary>
    public static DateTime TimestampToDataTime(long timeStamp)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).LocalDateTime;
    }

    /// <summary>
    /// 时间戳转本地时间字符串
    /// </summary>
    public static string TimestampToDataTimeString(long timeStamp)
    {
        var dateTime = TimestampToDataTime(timeStamp);
        return dateTime.ToString("yyyy/MM/dd HH:mm:ss");
    }

    /// <summary>
    /// 返回时间戳相差天数
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <returns></returns>
    public static int GetDiffDays(long timeStamp)
    {
        var dateTime = TimestampToDataTime(timeStamp);
        var daysSpan = new TimeSpan(DateTime.Now.Ticks - dateTime.Ticks);
        return daysSpan.Days;
    }
}
