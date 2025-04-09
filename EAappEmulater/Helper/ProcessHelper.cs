using System.Security.Policy;

namespace EAappEmulater.Helper;

public static class ProcessHelper
{
    /// <summary>
    /// 判断进程是否运行
    /// </summary>
    public static bool IsAppRun(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            return false;

        if (appName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            appName = appName[..^4];

        return Process.GetProcessesByName(appName).Length > 0;
    }

    /// <summary>
    /// 打开http链接
    /// </summary>
    public static void OpenLink(string url)
    {
        if (!url.StartsWith("http"))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Helper.ProcessHelper.OpenLinkFormatError", url));
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Helper.ProcessHelper.OpenLinkError", url, ex));
        }
    }

    /// <summary>
    /// 打开文件夹路径
    /// </summary>
    public static void OpenDirectory(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Helper.ProcessHelper.OpenDirectoryFormatError", dirPath));
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(dirPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Helper.ProcessHelper.OpenDirectoryError", dirPath, ex));
        }
    }

    /// <summary>
    /// 打开指定进程，使用explorer来启动避免继承管理员身份
    /// </summary>
    public static void OpenProcess(string appPath, bool isSilent = false)
    {
        if (!File.Exists(appPath))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Helper.ProcessHelper.OpenProcessFormatError", appPath));
            return;
        }
        var fileInfo = new FileInfo(appPath);
        try
        {
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "explorer.exe",
                Arguments = $"\"{fileInfo.FullName}\"",
                WorkingDirectory = fileInfo.DirectoryName,
            };
            if (isSilent)
            {
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processInfo.CreateNoWindow = true;
            }
            Process.Start(processInfo);
            LoggerHelper.Info(I18nHelper.I18n._("Helper.ProcessHelper.OpenProcessSuccess", appPath));
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Helper.ProcessHelper.OpenProcessError", appPath, ex));
        }
    }

    /// <summary>
    /// 根据名字关闭指定进程
    /// </summary>
    public static void CloseProcess(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            return;

        if (appName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            appName = appName[..^4];

        try
        {
            var isFind = false;

            foreach (var process in Process.GetProcessesByName(appName))
            {
                // 关闭进程树
                process.Kill(true);
                LoggerHelper.Info(I18nHelper.I18n._("Helper.ProcessHelper.CloseProcessSuccess", appName));

                isFind = true;
            }

            if (!isFind)
                LoggerHelper.Warn(I18nHelper.I18n._("Helper.ProcessHelper.CloseProcessErrorNotFound", appName));
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Helper.ProcessHelper.CloseProcessError", appName, ex));
        }
    }
}
