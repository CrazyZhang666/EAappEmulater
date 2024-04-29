﻿using EAappEmulater.Extend;
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
            Layout = "${longdate} ${level:upperCase=true} ${message} ${exception:format=message}",
            MaxArchiveFiles = 30,
            ArchiveAboveSize = 1024 * 1024 * 10,
            ArchiveEvery = FileArchivePeriod.Day,
            Encoding = Encoding.UTF8
        };

#if DEBUG
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, new NlogViewerTarget());
#else
        config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
        config.AddRule(LogLevel.Info, LogLevel.Fatal, new NlogViewerTarget());
#endif

        LogManager.ThrowExceptions = false;
        LogManager.Configuration = config;
    }

    #region Trace，追踪
    public static void Trace(string msg)
    {
        Logger.Trace(msg);
    }

    public static void Trace(string msg, Exception err)
    {
        Logger.Trace(err, msg);
    }
    #endregion

    #region Debug，调试
    public static void Debug(string msg)
    {
        Logger.Debug(msg);
    }

    public static void Debug(string msg, Exception err)
    {
        Logger.Debug(err, msg);
    }
    #endregion

    #region Info，信息
    public static void Info(string msg)
    {
        Logger.Info(msg);
    }

    public static void Info(string msg, Exception err)
    {
        Logger.Info(err, msg);
    }
    #endregion

    #region Warn，警告
    public static void Warn(string msg)
    {
        Logger.Warn(msg);
    }

    public static void Warn(string msg, Exception err)
    {
        Logger.Warn(err, msg);
    }
    #endregion

    #region Error，错误
    public static void Error(string msg)
    {
        Logger.Error(msg);
    }

    public static void Error(string msg, Exception err)
    {
        Logger.Error(err, msg);
    }
    #endregion

    #region Fatal,致命错误
    public static void Fatal(string msg)
    {
        Logger.Fatal(msg);
    }

    public static void Fatal(string msg, Exception err)
    {
        Logger.Fatal(err, msg);
    }
    #endregion
}