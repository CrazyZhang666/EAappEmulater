using EAappEmulater.Models;

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
        try
        {
            string locale = string.Empty;

            Dictionary<string, string[]> gameInfoMap = new Dictionary<string, string[]>()
            {
                ["71067"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield 3", "SOFTWARE\\EA Games\\Battlefield 3" },
                ["1015362"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield 4", "SOFTWARE\\EA Games\\Battlefield 4" },
                ["1013920"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\BFH", "SOFTWARE\\EA Games\\BFH" },
                ["1026023"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield 1", "SOFTWARE\\EA Games\\Battlefield 1" },
                ["196216"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield V", "SOFTWARE\\EA Games\\Battlefield V" },
                ["193874"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield 2042", "SOFTWARE\\EA Games\\Battlefield 2042" },
                ["16115019"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Sports\\FIFA 23", "SOFTWARE\\EA Sports\\FIFA 23" },
                ["1026482"] = new string[] { "SOFTWARE\\WOW6432Node\\PopCap\\Plants vs Zombies GW2", "SOFTWARE\\PopCap\\Plants vs Zombies GW2" },
                ["1035052"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\STAR WARS Battlefront II", "SOFTWARE\\EA Games\\STAR WARS Battlefront II" },
                ["198235"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Sports\\EA SPORTS FC 24", "SOFTWARE\\EA Sports\\EA SPORTS FC 24" },
                ["16425635"] = new string[] { "SOFTWARE\\WOW6432Node\\Codemasters\\F1_23", "SOFTWARE\\Codemasters\\F1_23" },
                ["196787"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\Need for Speed Unbound", "SOFTWARE\\EA Games\\Need for Speed Unbound" },
                ["1039093"] = new string[] { "SOFTWARE\\WOW6432Node\\Respawn\\Titanfall2", "SOFTWARE\\Respawn\\Titanfall2" },
                ["195133"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\Need for Speed Heat", "SOFTWARE\\EA Games\\Need for Speed Heat" },
                ["194814"] = new string[] { "SOFTWARE\\WOW6432Node\\PopCap\\PVZ Battle for Neighborville", "SOFTWARE\\PopCap\\PVZ Battle for Neighborville" },
                ["16050355"] = new string[] { "SOFTWARE\\WOW6432Node\\Hazelight\\ItTakesTwo", "SOFTWARE\\Hazelight\\ItTakesTwo" },
                ["196837"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Sports\\FIFA 22", "SOFTWARE\\EA Sports\\FIFA 22" },
                ["1024486"] = new string[] { "SOFTWARE\\WOW6432Node\\EA Games\\Need for Speed", "SOFTWARE\\EA Games\\Need for Speed" },
                ["1014748"] = new string[] { "SOFTWARE\\WOW6432Node\\PopCap\\Plants vs Zombies Garden Warfare", "SOFTWARE\\PopCap\\Plants vs Zombies Garden Warfare" },
                ["1024390"] = new string[] { "SOFTWARE\\EA Games\\STAR WARS Battlefront", "SOFTWARE\\WOW6432Node\\EA Games\\STAR WARS Battlefront" },
            };


            if (gameInfoMap.ContainsKey(contentId))
            {
                string[] gameInfo = gameInfoMap[contentId];
                for (int i = 1; i < gameInfo.Length; i++)
                {
                    using (RegistryKey key = Registry.LocalMachine)
                    {
                        using (RegistryKey subkey = key.OpenSubKey(gameInfo[i]))
                        {
                            if (subkey != null)
                            {
                                locale = subkey.GetValue("Locale") as string;
                                if (!string.IsNullOrEmpty(locale))
                                {
                                    return locale;
                                }
                            }
                        }
                    }
                }
            }

            return "en_US";
        }
        catch (Exception)
        {
            return "en_US";
        }
    }
}
