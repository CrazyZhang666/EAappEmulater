﻿using EAappEmulater.Enums;
using EAappEmulater.Helper;

namespace EAappEmulater.Core;

public static class Game
{
    /// <summary>
    /// 获取系统环境变量集合
    /// </summary>
    private static Dictionary<string, string> GetEnvironmentVariables()
    {
        var environmentVariables = new Dictionary<string, string>();
        foreach (DictionaryEntry dirEnity in Environment.GetEnvironmentVariables())
        {
            environmentVariables.Add(dirEnity.Key.ToString(), dirEnity.Value.ToString());
        }
        return environmentVariables;
    }

    /// <summary>
    /// 启动游戏
    /// </summary>
    public static void RunGame(GameType gameType, string webArgs = "", bool isNotice = true)
    {
        try
        {
            var gameInfo = Base.GameInfoDb[gameType];

            var execPath = Path.Combine(gameInfo.Dir, gameInfo.AppName);
            var execPath2 = Path.Combine(gameInfo.Dir2, gameInfo.AppName);

            if (!gameInfo.IsUseCustom)
            {
                if (string.IsNullOrWhiteSpace(gameInfo.Dir))
                {
                    LoggerHelper.Warn($"{gameType} 游戏路径为空，启动游戏终止 {gameInfo.Dir}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} 游戏路径为空，启动游戏终止");

                    return;
                }

                if (!File.Exists(execPath))
                {
                    LoggerHelper.Warn($"{gameType} 游戏主程序文件不存在，启动游戏终止 {execPath}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} 游戏主程序文件不存在，启动游戏终止");

                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(gameInfo.Dir2))
                {
                    LoggerHelper.Warn($"{gameType} 游戏路径为空，启动游戏终止 {gameInfo.Dir}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} 游戏路径为空，启动游戏终止");

                    return;
                }

                if (!File.Exists(execPath2))
                {
                    LoggerHelper.Warn($"{gameType} 游戏主程序文件不存在，启动游戏终止 {execPath2}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} 游戏主程序文件不存在，启动游戏终止");

                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(Account.OriginPCToken))
            {
                LoggerHelper.Warn($"{gameType} OriginPCToken 为空，启动游戏终止");
                if (isNotice)
                    NotifierHelper.Warning($"{gameType} OriginPCToken 为空，启动游戏终止");

                return;
            }

            LoggerHelper.Info($"{gameType} 正在启动游戏中...");
            if (isNotice)
                NotifierHelper.Notice($"{gameType} 正在启动游戏中...");

            // 获取当前进程所有环境变量名及其值
            var environmentVariables = GetEnvironmentVariables();

            // 直接赋值给字典，如果键已存在，则更新对应的值，否则则创建
            environmentVariables["EAFreeTrialGame"] = "false";
            environmentVariables["EAAuthCode"] = Account.OriginPCToken;
            environmentVariables["EALaunchOfflineMode"] = "false";
            environmentVariables["OriginSessionKey"] = "7102090b-ea9a-4531-9598-b2a7e943b544";
            environmentVariables["EAGameLocale"] = "zh_TW";
            environmentVariables["EALaunchEnv"] = "production";
            environmentVariables["EALaunchEAID"] = "Misaka_Mikoto_01";
            environmentVariables["EALicenseToken"] = "114514";
            environmentVariables["EAEntitlementSource"] = "EA";
            environmentVariables["EAUseIGOAPI"] = "1";
            environmentVariables["EALaunchUserAuthToken"] = Account.OriginPCToken;
            environmentVariables["EAGenericAuthToken"] = Account.OriginPCToken;
            environmentVariables["EALaunchCode"] = "unavailable";
            environmentVariables["EARtPLaunchCode"] = EaCrypto.GetRTPHandshakeCode();
            environmentVariables["EALsxPort"] = "3216";
            environmentVariables["EAEgsProxyIpcPort"] = "1705";
            environmentVariables["EASteamProxyIpcPort"] = "1704";
            environmentVariables["EAExternalSource"] = "EA";
            environmentVariables["EASecureLaunchTokenTemp"] = "1001006949032";
            environmentVariables["SteamAppId"] = "";
            environmentVariables["ContentId"] = gameInfo.ContentId;
            environmentVariables["EAConnectionId"] = gameInfo.ContentId;

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false
            };

            if (!gameInfo.IsUseCustom)
            {
                startInfo.FileName = Path.Combine(gameInfo.Dir, gameInfo.AppName);
                startInfo.WorkingDirectory = gameInfo.Dir;
                startInfo.Arguments = string.Concat(webArgs, " ", gameInfo.Args).Trim();
            }
            else
            {
                startInfo.FileName = Path.Combine(gameInfo.Dir2, gameInfo.AppName);
                startInfo.WorkingDirectory = gameInfo.Dir2;
                startInfo.Arguments = string.Concat(webArgs, " ", gameInfo.Args2).Trim();
            }

            foreach (var variable in environmentVariables)
            {
                startInfo.EnvironmentVariables[variable.Key] = variable.Value;
            }

            Process.Start(startInfo);

            LoggerHelper.Info($"启动游戏 {gameType} 成功");
            if (isNotice)
                NotifierHelper.Success($"启动游戏 {gameType} 成功");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"启动游戏发生异常 {gameType}", ex);
            if (isNotice)
                NotifierHelper.Error($"启动游戏发生异常 {gameType}\n{ex.Message}");
        }
    }
}
