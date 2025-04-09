using EAappEmulater.Core;
using EAappEmulater.Enums;
using EAappEmulater.Helper;
using EAappEmulater.Utils;

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
    public static string QueryPresenceString { get; set; } = string.Empty;

    /// <summary>
    /// 程序主体语言, 默认跟随系统.
    /// </summary>
    public static string Language { get; set; } = string.Empty;

    public static string DefaultLanguage { get; set; } = string.Empty;


    static Globals()
    {
        _configPath = Path.Combine(CoreUtil.Dir_Config, "Config.ini");
    }

    /// <summary>
    /// 读取全局配置文件
    /// </summary>
    public static void Read()
    {
        LoggerHelper.Info(I18nHelper.I18n._("Globals.ReadConfig"));

        var slot = IniHelper.ReadString("Globals", "AccountSlot", _configPath);
        var defaultLanguage = IniHelper.ReadString("Globals", "lang", _configPath);

        LoggerHelper.Info(I18nHelper.I18n._("Globals.CurrentConfigPath", _configPath));
        LoggerHelper.Info(I18nHelper.I18n._("Globals.ReadConfigSuccess", slot));

        if (Enum.TryParse(slot, out AccountSlot accountSlot))
        {
            AccountSlot = accountSlot;
            LoggerHelper.Info(I18nHelper.I18n._("Globals.EnumTryParseSuccess", AccountSlot));
        }
        else
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Globals.EnumTryParseError", slot));
        }


        if (defaultLanguage == "zh-CN" || defaultLanguage == "en-US")
        {
            DefaultLanguage = defaultLanguage;
            LoggerHelper.Info(I18nHelper.I18n._("Globals.SetDefaultLanguageSuccess", DefaultLanguage));
        }
        else
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Globals.SetDefaultLanguageError"));
        }

        LoggerHelper.Info(I18nHelper.I18n._("Globals.ReadGlobalConfigSuccess"));
    }

    /// <summary>
    /// 写入全局配置文件
    /// </summary>
    public static void Write()
    {
        LoggerHelper.Info(I18nHelper.I18n._("Globals.SaveGlobalConfigProcess"));

        IniHelper.WriteString("Globals", "AccountSlot", $"{AccountSlot}", _configPath);
        LoggerHelper.Info(I18nHelper.I18n._("Globals.SaveGlobalConfigPath", _configPath));
        LoggerHelper.Info(I18nHelper.I18n._("Globals.SaveGlobalConfigSuccess"));
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
