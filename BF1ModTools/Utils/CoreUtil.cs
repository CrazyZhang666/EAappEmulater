using BF1ModTools.Helper;
using Microsoft.Web.WebView2.Core;

namespace BF1ModTools.Utils;

public static class CoreUtil
{
    #region 配置目录
    public static string Dir_MyDocuments { get; private set; }
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
    #endregion

    #region 数据目录
    public static string File_AppData { get; private set; }
    public static string File_Config_ManagerConfig { get; private set; }
    public static string Dir_Mods_Bf1 { get; private set; }

    public static string File_FrostyMod_FrostyModManager { get; private set; }
    public static string File_Marne_MarneDll { get; private set; }
    public static string File_Marne_MarneLauncher { get; private set; }
    public static string File_BattlefieldChat { get; private set; }
    #endregion

    public const string Name_BF1 = "bf1";

    public const string Name_FrostyModManager = "FrostyModManager";
    public const string Name_MarneLauncher = "MarneLauncher";
    public const string Name_BattlefieldChat = "BattlefieldChat";

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
        #endregion

        #region 数据目录
        File_AppData = Path.Combine(Dir_AppData, "AppData.zip");
        File_Config_ManagerConfig = Path.Combine(Dir_Config, "manager_config.json");
        Dir_Mods_Bf1 = Path.Combine(Dir_Mods, "bf1");

        File_FrostyMod_FrostyModManager = Path.Combine(Dir_AppData, "FrostyMod\\FrostyModManager.exe");
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
    public static async Task CloseServiceProcess()
    {
        LoggerHelper.Info("准备关闭服务进程");

        await ProcessHelper.CloseProcess("EADesktop");
        await ProcessHelper.CloseProcess("Origin");
        await ProcessHelper.CloseProcess("OriginDebug");

        await ProcessHelper.CloseProcess("FrostyModManager");
        await ProcessHelper.CloseProcess("MarneLauncher");
        await ProcessHelper.CloseProcess("BattlefieldChat");
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
    /// 是否运行在临时文件夹（压缩包内）
    /// </summary>
    /// <returns></returns>
    public static bool IsRunInTempPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var tempDir = Path.GetTempPath();

        LoggerHelper.Debug($"程序当前目录 {baseDir}");
        LoggerHelper.Debug($"相同临时目录 {tempDir}");

        return baseDir.StartsWith(tempDir, StringComparison.OrdinalIgnoreCase);
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
    public static async Task<bool> IsBf1MainAppFile(string bf1Path)
    {
        if (string.IsNullOrWhiteSpace(bf1Path))
            return false;

        if (!File.Exists(bf1Path))
            return false;

        return await FileHelper.GetFileMD5(bf1Path) == "190075FC83A4782EDDAFAADAE414391F";
    }

    /// <summary>
    /// 获取战地1安装路径
    /// </summary>
    public static async Task<bool> GetBf1InstallPath(bool isReSelect = false)
    {
        // 检查战地1路径是否为空
        if (!isReSelect && !string.IsNullOrWhiteSpace(Globals.BF1AppPath))
            return true;

        // 战地1路径无效，重新选择
        var dialog = new OpenFileDialog
        {
            Title = "请选择战地1游戏主程序 bf1.exe 文件路径",
            FileName = "bf1.exe",
            DefaultExt = ".exe",
            Filter = "可执行文件 (.exe)|*.exe",
            Multiselect = false,
            InitialDirectory = Globals.DialogDir,
            RestoreDirectory = true,
            AddExtension = true,
            CheckFileExists = true,
            CheckPathExists = true
        };

        // 如果未选择，则退出程序
        if (dialog.ShowDialog() == false)
            return false;

        var dirPath = Path.GetDirectoryName(dialog.FileName);
        // 记住本次选择的文件路径
        Globals.DialogDir = dirPath;

        // 开始校验文件有效性
        if (await IsBf1MainAppFile(dialog.FileName))
        {
            var diskFlag = Path.GetPathRoot(dirPath);
            var driveInfo = new DriveInfo(diskFlag);
            if (!driveInfo.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase))
            {
                LoggerHelper.Info($"检测到战地1所在磁盘格式不是NTFS，请转换磁盘格式 {Globals.BF1AppPath}");
                NotifierHelper.Warning("检测到战地1所在磁盘格式不是NTFS，请转换磁盘格式");
                return false;
            }

            Globals.SetBF1AppPath(dialog.FileName);
            LoggerHelper.Info($"获取战地1游戏主程序路径成功 {dialog.FileName}");
            NotifierHelper.Success("获取战地1游戏主程序路径成功");
            return true;
        }

        LoggerHelper.Warn($"战地1游戏主程序路径无效，请重新选择 {dialog.FileName}");
        NotifierHelper.Warning($"战地1游戏主程序路径无效，请重新选择");
        return false;
    }
}
