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

    /// <summary>
    /// 是否启用自动登录
    /// </summary>
    public static bool AutoLoginEnabled { get; set; } = false;

    /// <summary>
    /// 关闭窗口时是否最小化到托盘（true=最小化到托盘，false=完全退出）
    /// </summary>
    public static bool CloseToTray { get; set; } = true;

    /// <summary>
    /// 命令行指定的账号槽（如果为null则使用配置文件）
    /// </summary>
    public static AccountSlot? CommandLineAccountSlot { get; set; } = null;

    /// <summary>
    /// 命令行指定的游戏类型（如果为null则不自动启动游戏）
    /// </summary>
    public static GameType? CommandLineGameType { get; set; } = null;

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
        var autoLoginEnabled = IniHelper.ReadString("Globals", "AutoLoginEnabled", _configPath);
        var closeToTray = IniHelper.ReadString("Globals", "CloseToTray", _configPath);

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


        // Accept any configured language if it's in the supported language list
        if (!string.IsNullOrWhiteSpace(defaultLanguage))
        {
            try
            {
                var langEntry = LanguageConfigHelper.FindByCode(defaultLanguage);
                if (langEntry != null)
                {
                    DefaultLanguage = defaultLanguage;
                    LoggerHelper.Info(I18nHelper.I18n._("Globals.SetDefaultLanguageSuccess", DefaultLanguage));
                }
                else
                {
                    LoggerHelper.Warn(I18nHelper.I18n._("Globals.SetDefaultLanguageError"));
                }
            }
            catch
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Globals.SetDefaultLanguageError"));
            }
        }

        if (!string.IsNullOrWhiteSpace(autoLoginEnabled))
        {
            if (bool.TryParse(autoLoginEnabled, out bool autoLogin))
            {
                AutoLoginEnabled = autoLogin;
                LoggerHelper.Info(I18nHelper.I18n._("Globals.AutoLoginSetting", AutoLoginEnabled));
            }
            else
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Globals.AutoLoginParseError", autoLoginEnabled));
            }
        }
        else
        {
            LoggerHelper.Info(I18nHelper.I18n._("Globals.AutoLoginNotConfigured"));
        }

        if (!string.IsNullOrWhiteSpace(closeToTray))
        {
            if (bool.TryParse(closeToTray, out bool closeToTrayValue))
            {
                CloseToTray = closeToTrayValue;
                LoggerHelper.Info($"关闭窗口行为: {(CloseToTray ? "最小化到托盘" : "完全退出")}");
            }
            else
            {
                LoggerHelper.Warn($"关闭窗口行为解析失败: {closeToTray}");
            }
        }

        LoggerHelper.Info(I18nHelper.I18n._("Globals.ReadGlobalConfigSuccess"));
    }

    /// <summary>
    /// 写入全局配置文件
    /// </summary>
    public static void Write()
    {
        LoggerHelper.Info(I18nHelper.I18n._("Globals.SaveGlobalConfigProcess"));

        try
        {
            // ensure config dir and file exist
            FileHelper.CreateDirectory(CoreUtil.Dir_Config);
            FileHelper.CreateFile(_configPath);

            IniHelper.WriteString("Globals", "AccountSlot", $"{AccountSlot}", _configPath);
            IniHelper.WriteString("Globals", "lang", DefaultLanguage ?? string.Empty, _configPath);
            IniHelper.WriteString("Globals", "AutoLoginEnabled", $"{AutoLoginEnabled}", _configPath);
            IniHelper.WriteString("Globals", "CloseToTray", $"{CloseToTray}", _configPath);

            LoggerHelper.Info(I18nHelper.I18n._("Globals.SaveGlobalConfigPath", _configPath));
            LoggerHelper.Info(I18nHelper.I18n._("Globals.SaveGlobalConfigSuccess"));
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Globals.SaveGlobalConfigError", ex));
        }
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