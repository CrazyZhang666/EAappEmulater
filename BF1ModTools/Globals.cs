using BF1ModTools.Core;
using BF1ModTools.Utils;
using BF1ModTools.Helper;

namespace BF1ModTools;

public static class Globals
{
    private static readonly string _iniPath;

    ///////////////////////////////////

    public static string BF1InstallDir { get; set; }

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

    public static void Read()
    {
        LoggerHelper.Info("开始读取配置文件...");

        Account.Read();

        BF1InstallDir = ReadString("BF1", "InstallDir");

        LoggerHelper.Info("读取配置文件成功");
    }

    public static void Write()
    {
        LoggerHelper.Info("开始保存配置文件...");

        Account.Write();

        WriteString("BF1", "InstallDir", BF1InstallDir);

        LoggerHelper.Info("保存配置文件成功");
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
