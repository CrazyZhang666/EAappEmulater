﻿using BF1ModTools.Helper;
using Microsoft.Web.WebView2.Core;

namespace BF1ModTools.Utils;

public static class CoreUtil
{
    #region 配置目录
    public static string Dir_MyDocuments { get; private set; }
    public static string Dir_Default { get; private set; }

    public static string Dir_Cache { get; private set; }
    public static string Dir_Config { get; private set; }
    public static string Dir_Log { get; private set; }

    public static string Dir_Log_Crash { get; private set; }
    public static string Dir_Log_NLog { get; private set; }
    #endregion

    #region 数据目录
    public static string Dir_AppData { get; private set; }

    public static string Dir_FrostyMod_Mods_Bf1 { get; private set; }

    public static string File_FrostyMod_FrostyModManager { get; private set; }
    public static string File_FrostyMod_Frosty_ManagerConfig { get; private set; }
    public static string File_Marne_MarneDll { get; private set; }
    public static string File_Marne_MarneLauncher { get; private set; }
    public static string File_BattlefieldChat { get; private set; }
    #endregion

    public const string Name_FrostyModManager = "FrostyModManager";
    public const string Name_MarneLauncher = "MarneLauncher";

    public const string Name_BF1 = "bf1";

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
        Dir_MyDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        Dir_Default = Path.Combine(Dir_MyDocuments, "BF1ModTools");

        Dir_Cache = Path.Combine(Dir_Default, "Cache");
        Dir_Config = Path.Combine(Dir_Default, "Config");
        Dir_Log = Path.Combine(Dir_Default, "Log");

        Dir_Log_Crash = Path.Combine(Dir_Log, "Crash");
        Dir_Log_NLog = Path.Combine(Dir_Log, "NLog");

        FileHelper.CreateDirectory(Dir_Cache);
        FileHelper.CreateDirectory(Dir_Config);
        FileHelper.CreateDirectory(Dir_Log);

        FileHelper.CreateDirectory(Dir_Log_Crash);
        FileHelper.CreateDirectory(Dir_Log_NLog);
        #endregion

        #region 数据目录
        Dir_AppData = Path.Combine(Environment.CurrentDirectory, "AppData\\");

        Dir_FrostyMod_Mods_Bf1 = Path.Combine(Dir_AppData, "FrostyMod\\Mods\\bf1\\");

        File_FrostyMod_FrostyModManager = Path.Combine(Dir_AppData, "FrostyMod\\FrostyModManager.exe");
        File_FrostyMod_Frosty_ManagerConfig = Path.Combine(Dir_AppData, "FrostyMod\\Frosty\\manager_config.json");
        File_Marne_MarneLauncher = Path.Combine(Dir_AppData, "Marne\\MarneLauncher.exe");
        File_Marne_MarneDll = Path.Combine(Dir_AppData, "Marne\\Marne.dll");
        File_BattlefieldChat = Path.Combine(Dir_AppData, "Tools\\BattlefieldChat.exe");
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
    public static async Task CloseServerProcess()
    {
        LoggerHelper.Info("准备关闭服务进程");
        await ProcessHelper.CloseProcess("EADesktop");
        await ProcessHelper.CloseProcess("OriginDebug");
        await ProcessHelper.CloseProcess("Origin");
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
    /// 保存崩溃日志
    /// </summary>
    public static void SaveCrashLog(string log)
    {
        try
        {
            var path = Path.Combine(Dir_Log_Crash, $"CrashReport-{DateTime.Now:yyyyMMdd_HHmmss_ffff}.log");
            File.WriteAllText(path, log);
        }
        catch { }
    }
}
