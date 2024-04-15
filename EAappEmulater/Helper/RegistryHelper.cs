namespace EAappEmulater.Helper;

public static class RegistryHelper
{
    /// <summary>
    /// 读取注册表
    /// </summary>
    public static string GetRegistryTargetVaule(string regPath, string keyName)
    {
        try
        {
            var localMachine = Registry.LocalMachine;

            using var regKey = localMachine.OpenSubKey(regPath);
            if (regKey is null)
                return string.Empty;

            return regKey.GetValue(keyName).ToString();
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"读取注册表异常 {regPath} {keyName}", ex);
            return string.Empty;
        }
    }

    /// <summary>
    /// 写入注册表
    /// </summary>
    public static void SetRegistryTargetVaule(string regPath, string keyName, string value)
    {
        try
        {
            var localMachine = Registry.LocalMachine;

            // 创建注册表，如果已经存在则不影响
            using var regKey = localMachine.CreateSubKey(regPath, true);
            if (regKey is null)
                return;

            regKey.SetValue(keyName, value);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"写入注册表异常 {regPath} {keyName} {value}", ex);
        }
    }

    /// <summary>
    /// 读取注册表EA游戏安装目录
    /// </summary>
    public static string GetRegistryInstallDir(string regPath)
    {
        var dirPath = GetRegistryTargetVaule(regPath, "Install Dir");
        if (string.IsNullOrWhiteSpace(dirPath))
            return string.Empty;

        return Directory.Exists(dirPath) ? dirPath : string.Empty;
    }

    /// <summary>
    /// 读取注册表EA游戏语言信息
    /// </summary>
    /// <param name="regPath"></param>
    /// <returns></returns>
    public static string GetRegistryLocale(string regPath)
    {
        var dirPath = GetRegistryTargetVaule(regPath, "Locale");
        if (string.IsNullOrWhiteSpace(dirPath))
            return string.Empty;

        return dirPath;
    }
}
