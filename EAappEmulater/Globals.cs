using EAappEmulater.Core;
using EAappEmulater.Enums;
using EAappEmulater.Utils;
using EAappEmulater.Helper;

namespace EAappEmulater;

public static class Globals
{
    /// <summary>
    /// 全局配置文件路径
    /// </summary>
    private static readonly string _configPath;

    /// <summary>
    /// 当前使用的账号槽
    /// </summary>
    public static AccountSlot AccountSlot { get; set; } = AccountSlot.S0;

    public static bool IsGetFriendsSuccess { get; set; } = false;
    public static string FriendsXmlString { get; set; } = string.Empty;

    static Globals()
    {
        _configPath = Path.Combine(CoreUtil.Dir_Config, "Config.ini");
    }

    /// <summary>
    /// 读取全局配置文件
    /// </summary>
    public static void Read()
    {
        LoggerHelper.Info("开始读取全局配置文件...");

        var slot = IniHelper.ReadString("Globals", "AccountSlot", _configPath);
        LoggerHelper.Info($"当前读取配置文件路径 {_configPath}");
        LoggerHelper.Info($"读取配置文件成功 Globals AccountSlot {slot}");

        if (Enum.TryParse(slot, out AccountSlot accountSlot))
        {
            AccountSlot = accountSlot;
            LoggerHelper.Info($"枚举转换成功 AccountSlot {AccountSlot}");
        }
        else
        {
            LoggerHelper.Warn($"枚举转换失败 AccountSlot {slot}");
        }

        LoggerHelper.Info("读取全局配置文件成功");
    }

    /// <summary>
    /// 写入全局配置文件
    /// </summary>
    public static void Write()
    {
        LoggerHelper.Info("开始保存全局配置文件...");

        IniHelper.WriteString("Globals", "AccountSlot", $"{AccountSlot}", _configPath);
        LoggerHelper.Info($"当前保存配置文件路径 {_configPath}");
        LoggerHelper.Info($"保存配置文件成功 Globals AccountSlot {AccountSlot}");

        LoggerHelper.Info("保存全局配置文件成功");
    }

    /// <summary>
    /// 获取当前账号槽全局配置文件路径
    /// </summary>
    public static string GetAccountIniPath()
    {
        return Account.AccountPathDb[AccountSlot];
    }

    /// <summary>
    /// 获取当前账号槽WebView2缓存路径
    /// </summary>
    public static string GetAccountCacheDir()
    {
        return CoreUtil.AccountCacheDb[AccountSlot];
    }
}
