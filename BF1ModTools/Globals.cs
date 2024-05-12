using BF1ModTools.Core;
using BF1ModTools.Utils;
using BF1ModTools.Helper;

namespace BF1ModTools;

public static class Globals
{
    private static readonly string _iniPath;

    // 这个不保存配置文件
    public static string BF1InstallDir { get; private set; }

    ///////////////////////////////////

    public static string BF1AppPath { get; private set; }

    ///////////////////////////////////

    public static string DialogDir
    {
        get => ReadString("Dialog", "InitDir");
        set => WriteString("Dialog", "InitDir", value);
    }

    public static string DialogDir2
    {
        get => ReadString("Dialog", "InitDir2");
        set => WriteString("Dialog", "InitDir2", value);
    }

    static Globals()
    {
        _iniPath = Path.Combine(CoreUtil.Dir_Config, "Config.ini");
        LoggerHelper.Info($"当前重置配置文件路径 {_iniPath}");
    }

    /// <summary>
    /// 读取全局配置文件
    /// </summary>
    public static async Task Read()
    {
        LoggerHelper.Info("开始读取配置文件...");

        Account.Read();

        var appPath = ReadString("BF1", "AppPath");
        if (await CoreUtil.IsBf1MainAppFile(appPath))
        {
            SetBF1AppPath(appPath);
            LoggerHelper.Info($"已发现战地1安装路径 {BF1AppPath}");
        }
        else
        {
            SetBF1AppPath(string.Empty);
            LoggerHelper.Warn($"未发现战地1安装路径，请手动选择");
        }

        LoggerHelper.Info("读取配置文件成功");
    }

    /// <summary>
    /// 写入全局配置文件
    /// </summary>
    public static void Write()
    {
        LoggerHelper.Info("开始保存配置文件...");

        Account.Write();

        WriteString("BF1", "AppPath", BF1AppPath);

        LoggerHelper.Info("保存配置文件成功");
    }

    /// <summary>
    /// 设置战地1主程序路径
    /// </summary>
    public static void SetBF1AppPath(string appPath)
    {
        if (string.IsNullOrWhiteSpace(appPath))
        {
            BF1AppPath = string.Empty;
            BF1InstallDir = string.Empty;
            return;
        }

        BF1AppPath = appPath;
        BF1InstallDir = Path.GetDirectoryName(appPath);
    }

    private static string ReadString(string section, string key)
    {
        return IniHelper.ReadString(section, key, _iniPath);
    }

    private static void WriteString(string section, string key, string value)
    {
        IniHelper.WriteString(section, key, value, _iniPath);
    }
}
