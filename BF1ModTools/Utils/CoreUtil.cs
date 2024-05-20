using BF1ModTools.Helper;
using Microsoft.Web.WebView2.Core;

namespace BF1ModTools.Utils;

public static class CoreUtil
{
    #region 配置目录
    public static string Dir_CommonAppData { get; private set; }
    public static string Dir_Default { get; private set; }

    public static string Dir_AppData { get; private set; }
    public static string Dir_Mods { get; private set; }

    public static string Dir_Cache { get; private set; }
    public static string Dir_Config { get; private set; }
    public static string Dir_Service { get; private set; }
    public static string Dir_Log { get; private set; }

    public static string Dir_Log_Crash { get; private set; }
    public static string Dir_Log_NLog { get; private set; }

    public static string File_Service_EADesktop { get; private set; }
    public static string File_Service_OriginDebug { get; private set; }
    public static string File_Service_BF1Chat { get; private set; }
    #endregion

    #region 数据目录
    public static string File_AppData { get; private set; }
    public static string File_Config_ManagerConfig { get; private set; }
    public static string Dir_Mods_Bf1 { get; private set; }

    public static string File_FrostyMod_FrostyModManager { get; private set; }
    public static string File_Marne_MarneDll { get; private set; }
    public static string File_Marne_MarneLauncher { get; private set; }
    #endregion

    public const string Name_BF1 = "bf1";

    public const string Name_FrostyModManager = "FrostyModManager";
    public const string Name_MarneLauncher = "MarneLauncher";

    //////////////////////////////////

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
        Dir_CommonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        Dir_Default = Path.Combine(Dir_CommonAppData, "BF1ModTools");

        Dir_AppData = Path.Combine(Dir_Default, "AppData");
        Dir_Mods = Path.Combine(Dir_Default, "Mods");

        Dir_Cache = Path.Combine(Dir_Default, "Cache");
        Dir_Config = Path.Combine(Dir_Default, "Config");
        Dir_Service = Path.Combine(Dir_Default, "Service");
        Dir_Log = Path.Combine(Dir_Default, "Log");

        Dir_Log_Crash = Path.Combine(Dir_Log, "Crash");
        Dir_Log_NLog = Path.Combine(Dir_Log, "NLog");

        FileHelper.CreateDirectory(Dir_AppData);
        FileHelper.CreateDirectory(Dir_Mods);

        FileHelper.CreateDirectory(Dir_Cache);
        FileHelper.CreateDirectory(Dir_Config);
        FileHelper.CreateDirectory(Dir_Service);
        FileHelper.CreateDirectory(Dir_Log);

        FileHelper.CreateDirectory(Dir_Log_Crash);
        FileHelper.CreateDirectory(Dir_Log_NLog);

        File_Service_EADesktop = Path.Combine(Dir_Service, "EADesktop.exe");
        File_Service_OriginDebug = Path.Combine(Dir_Service, "OriginDebug.exe");
        File_Service_BF1Chat = Path.Combine(Dir_Service, "BF1Chat.exe");
        #endregion

        #region 数据目录
        File_AppData = Path.Combine(Dir_AppData, "AppData.zip");
        File_Config_ManagerConfig = Path.Combine(Dir_Config, "manager_config.json");
        Dir_Mods_Bf1 = Path.Combine(Dir_Mods, "bf1");

        File_FrostyMod_FrostyModManager = Path.Combine(Dir_AppData, "FrostyMod\\FrostyModManager.exe");
        File_Marne_MarneLauncher = Path.Combine(Dir_AppData, "Marne\\MarneLauncher.exe");
        File_Marne_MarneDll = Path.Combine(Dir_AppData, "Marne\\Marne.dll");
        #endregion

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
    public static async Task CloseServiceProcess()
    {
        LoggerHelper.Info("准备关闭服务进程");

        await ProcessHelper.CloseProcess("EADesktop");
        await ProcessHelper.CloseProcess("Origin");
        await ProcessHelper.CloseProcess("OriginDebug");

        await ProcessHelper.CloseProcess("BF1Chat");
        await ProcessHelper.CloseProcess("EAappEmulater");

        await ProcessHelper.CloseProcess("FrostyModManager");
        await ProcessHelper.CloseProcess("MarneLauncher");
    }

    /// <summary>
    /// 检查WebView2依赖
    /// </summary>
    public static bool CheckWebView2Env()
    {
        try
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            LoggerHelper.Info($"WebView2 Runtime 版本信息 {version}");
            return !string.IsNullOrWhiteSpace(version);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("WebView2 Runtime 环境异常", ex);
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
    /// 获取是否管理员运行字符串
    /// </summary>
    public static string GetIsAdminStr()
    {
        return IsRunAsAdmin() ? "管理员" : "非管理员";
    }

    /// <summary>
    /// 检测是否为战地1主程序文件
    /// </summary>
    public static bool IsBf1MainAppFile(string bf1Path)
    {
        // 判断路径是否为空
        if (string.IsNullOrWhiteSpace(bf1Path))
            return false;

        // 判断文件是否存在
        if (!File.Exists(bf1Path))
            return false;

        // 判断文件名称
        if (Path.GetFileName(bf1Path) != "bf1.exe")
            return false;

        // 判断文件详细信息
        var fileVerInfo = FileVersionInfo.GetVersionInfo(bf1Path);

        if (fileVerInfo.CompanyName != "EA Digital Illusions CE AB")
            return false;
        if (fileVerInfo.FileDescription != "Battlefield™ 1")
            return false;
        if (fileVerInfo.FileVersion != "1, 0, 57, 44284")
            return false;
        if (fileVerInfo.LegalCopyright != "Copyright © 2016 EA Digital Illusions CE AB. All rights reserved.")
            return false;

        return true;
    }
}
