namespace BF1Chat.Helper;

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
}
