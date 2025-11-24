using EAappEmulater.Extend;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace EAappEmulater.Helper;

public static class LoggerHelper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static LoggerHelper()
    {
        var config = new LoggingConfiguration();

        var logfile = new FileTarget("logfile")
        {
            FileName = "${specialfolder:folder=MyDocuments}/EAappEmulater/Log/NLog/${shortdate}.log",
            Layout = "${longdate} ${level:upperCase=true} ${message}",
            MaxArchiveFiles = 30,
            ArchiveAboveSize = 1024 * 1024 * 10,
            ArchiveEvery = FileArchivePeriod.Day,
            Encoding = Encoding.UTF8
        };

        // 默认完整日志（已去除异常消息自动渲染，改为我们手动拼接并脱敏）
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, new NlogViewerTarget());

        LogManager.ThrowExceptions = false;
        LogManager.Configuration = config;
    }

    /// <summary>
    /// 全局日志脱敏，避免隐私/凭据泄露
    /// </summary>
    public static string Sanitize(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return msg;

        try
        {
            // 常见敏感键统一掩码
            // Cookie: remid, sid
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(?i)(remid\s*=)([^;\r\n\s]+)", "$1<redacted>");
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(?i)(sid\s*=)([^;\r\n\s]+)", "$1<redacted>");

            // Authorization Bearer token
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(?i)(Bearer)\s+[A-Za-z0-9\-._~+/]+=*", "$1 <redacted>");
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(?i)(Authorization\s*:\s*Bearer)\s+[A-Za-z0-9\-._~+/]+=*", "$1 <redacted>");

            // access_token in JSON or query
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(?i)(access_token""?\s*[:=]\s*""?)([A-Za-z0-9\-._~+/]+=*)", "$1<redacted>");

            // LSX / EA tokens
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(?i)(OriginPCToken|OriginPCAuth|AccessToken|LSXAuthCode)""?\s*[:=]\s*""?([^&""\s]+)", "$1=<redacted>");

            // Generic Cookie header
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(?i)(Cookie\s*[:=]\s*)([^\r\n]+)", "$1<redacted>");

            // OAuth code in query string (e.g., code=XYZ)
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(?i)([?&])code=([^&\s]+)", "$1code=<redacted>");

            // ea_eadmtoken in query
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(?i)(ea_eadmtoken=)([^&\s]+)", "$1<redacted>");
        }
        catch { }

        return msg;
    }

    private static string ComposeWithException(string msg, Exception err)
    {
        if (err == null) return Sanitize(msg);
        string combined = $"{msg} | {err.GetType().Name}: {err.Message}";
        return Sanitize(combined);
    }

    /// <summary>
    /// 设置日志最小等级
    /// </summary>
    public static void SetLogMinLevel(LogLevel minLevel)
    {
        var config = LogManager.Configuration;

        foreach (var item in config.LoggingRules)
        {
            item.SetLoggingLevels(minLevel, LogLevel.Fatal);
        }

        LogManager.ReconfigExistingLoggers();
    }

    #region Trace，追踪
    public static void Trace(string msg)
    {
        Logger.Trace(Sanitize(msg));
    }

    public static void Trace(string msg, Exception err)
    {
        Logger.Trace(ComposeWithException(msg, err));
    }
    #endregion

    #region Debug，调试
    public static void Debug(string msg)
    {
        Logger.Debug(Sanitize(msg));
    }

    public static void Debug(string msg, Exception err)
    {
        Logger.Debug(ComposeWithException(msg, err));
    }
    #endregion

    #region Info，信息
    public static void Info(string msg)
    {
        Logger.Info(Sanitize(msg));
    }

    public static void Info(string msg, Exception err)
    {
        Logger.Info(ComposeWithException(msg, err));
    }
    #endregion

    #region Warn，警告
    public static void Warn(string msg)
    {
        Logger.Warn(Sanitize(msg));
    }

    public static void Warn(string msg, Exception err)
    {
        Logger.Warn(ComposeWithException(msg, err));
    }
    #endregion

    #region Error，错误
    public static void Error(string msg)
    {
        Logger.Error(Sanitize(msg));
    }

    public static void Error(string msg, Exception err)
    {
        Logger.Error(ComposeWithException(msg, err));
    }
    #endregion

    #region Fatal,致命错误
    public static void Fatal(string msg)
    {
        Logger.Fatal(Sanitize(msg));
    }

    public static void Fatal(string msg, Exception err)
    {
        Logger.Fatal(ComposeWithException(msg, err));
    }
    #endregion
}