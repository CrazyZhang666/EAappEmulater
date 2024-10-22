using EAappEmulater.Core;

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

    /// <summary>
    /// 获取当前游戏安装语言
    /// </summary>
    public static string GetLocaleByContentId(string contentId)
    {
        if (Base.GameRegistryDb.TryGetValue(contentId, out List<string> regs))
        {
            foreach (var reg in regs)
            {
                var locale = GetRegistryTargetVaule(reg, "Locale");
                if (!string.IsNullOrWhiteSpace(locale))
                    return locale;
            }
        }

        return "en_US";
    }

    /// <summary>
    /// 获取当前游戏安装路径
    /// </summary>
    public static string GetInstallDirByContentId(string contentId)
    {
        if (Base.GameRegistryDb.TryGetValue(contentId, out List<string> regs))
        {
            foreach (var reg in regs)
            {
                var locale = GetRegistryTargetVaule(reg, "Install Dir");
                if (!string.IsNullOrWhiteSpace(locale))
                    return locale;
            }
        }

        return "";
    }

    /// <summary>
    /// 获取Origin/EA App注册表情况并且每次启动都直接写入
    /// </summary>
    public static void CheckAndAddEaAppRegistryKey()
    {
        /*
         * 这样可以解决F1 23等游戏不安装EA Desktop/Origin并且注册表ClientPath路径没有程序则没办法启动的问题
         * 还能顺便解决TTF2等游戏启动的时候会弹出EA App的问题
         */

        try
        {
            using var localMachine = Registry.LocalMachine;
            using var newSubKey = localMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Origin", true);

            if (newSubKey is not null)
            {
                var clientPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmdkey.exe");
                newSubKey.SetValue("ClientPath", clientPath, RegistryValueKind.String);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("写入 EADesktop 安装路径注册表异常", ex);
        }
    }
}
