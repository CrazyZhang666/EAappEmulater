using EAappEmulater.Enums;
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

            ////////////////////////////////////////////////////////

            var execPath = string.Empty;        // 注册表路径
            var execPath2 = string.Empty;       // 自定义启动路径

            // 处理 双人成行 特殊启动路径
            if (gameInfo.GameType is GameType.ITT)
            {
                // 双人成行
                execPath = Path.Combine(gameInfo.Dir, "Nuts\\Binaries\\Win64", gameInfo.AppName);
                execPath2 = Path.Combine(gameInfo.Dir2, "Nuts\\Binaries\\Win64", gameInfo.AppName);
            }
            else
            {
                // 其他
                execPath = Path.Combine(gameInfo.Dir, gameInfo.AppName);
                execPath2 = Path.Combine(gameInfo.Dir2, gameInfo.AppName);
            }

            // 判断是否使用自定义路径启动游戏
            if (gameInfo.IsUseCustom)
            {
                // 自定义游戏路径

                // 判断游戏路径
                if (string.IsNullOrWhiteSpace(gameInfo.Dir2))
                {
                    LoggerHelper.Warn($"{gameType} 游戏路径为空，启动游戏终止 {gameInfo.Dir}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} 游戏路径为空，启动游戏终止");

                    return;
                }

                // 判断游戏文件
                if (!File.Exists(execPath2))
                {
                    LoggerHelper.Warn($"{gameType} 游戏主程序文件不存在，启动游戏终止 {execPath2}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} 游戏主程序文件不存在，启动游戏终止");

                    return;
                }
            }
            else
            {
                // 注册表游戏路径

                // 判断游戏路径
                if (string.IsNullOrWhiteSpace(gameInfo.Dir))
                {
                    LoggerHelper.Warn($"{gameType} 游戏路径为空，启动游戏终止 {gameInfo.Dir}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} 游戏路径为空，启动游戏终止");

                    return;
                }

                // 判断游戏文件
                if (!File.Exists(execPath))
                {
                    LoggerHelper.Warn($"{gameType} 游戏主程序文件不存在，启动游戏终止 {execPath}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} 游戏主程序文件不存在，启动游戏终止");

                    return;
                }
            }

            ////////////////////////////////////////////////////////

            if (string.IsNullOrWhiteSpace(Account.OriginPCToken))
            {
                LoggerHelper.Warn($"{gameType} OriginPCToken 为空，启动游戏终止");
                if (isNotice)
                    NotifierHelper.Warning($"{gameType} OriginPCToken 为空，启动游戏终止");

                return;
            }

            ////////////////////////////////////////////////////////

            // 处理旧的 LSX
            if (gameInfo.IsOldLSX)
                BattlelogHttpServer.BattlelogType = BattlelogType.BFH;
            else
                switch (gameType)
                {
                    case GameType.BF3:
                        BattlelogHttpServer.BattlelogType = BattlelogType.BF3;
                        break;
                    case GameType.BF4:
                        BattlelogHttpServer.BattlelogType = BattlelogType.BF4;
                        break;
                    case GameType.BFH:
                        BattlelogHttpServer.BattlelogType = BattlelogType.BFH;
                        break;
                    default:
                        BattlelogHttpServer.BattlelogType = BattlelogType.None;
                        break;
                }

            LoggerHelper.Info($"{gameInfo.Name} 正在启动游戏中...");
            if (isNotice)
                NotifierHelper.Notice($"{gameInfo.Name} 正在启动游戏中...");

            // 获取当前进程所有环境变量名及其值
            var environmentVariables = GetEnvironmentVariables();

            // 直接赋值给字典，如果键已存在，则更新对应的值，否则则创建
            environmentVariables["EAFreeTrialGame"] = "false";
            environmentVariables["EAAuthCode"] = Account.OriginPCToken;
            environmentVariables["EALaunchOfflineMode"] = "false";
            environmentVariables["OriginSessionKey"] = "7102090b-ea9a-4531-9598-b2a7e943b544";
            environmentVariables["EAGameLocale"] = "zh_TW";
            environmentVariables["EALaunchEnv"] = "production";
            environmentVariables["EALaunchEAID"] = Account.PlayerName;
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

            // 修复泰坦陨落2无法连接数据中心，傻逼重生
            if (gameInfo.GameType is GameType.TTF2)
                environmentVariables["OPENSSL_ia32cap"] = "~0x200000200000000";

            // 初始化进程类实例
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false
            };

            // 判断是否使用自定义路径启动游戏
            if (gameInfo.IsUseCustom)
            {
                // 自定义游戏路径

                if (gameInfo.GameType is GameType.ITT)
                {
                    // 双人成行
                    startInfo.FileName = Path.Combine(gameInfo.Dir2, "Nuts\\Binaries\\Win64", gameInfo.AppName);
                    startInfo.WorkingDirectory = Path.Combine(gameInfo.Dir2, "Nuts\\Binaries\\Win64", gameInfo.AppName);
                }
                else
                {
                    // 其他
                    startInfo.FileName = Path.Combine(gameInfo.Dir2, gameInfo.AppName);
                    startInfo.WorkingDirectory = gameInfo.Dir2;
                }

                // 启动参数
                startInfo.Arguments = string.Concat(webArgs, " ", gameInfo.Args2).Trim();
            }
            else
            {
                // 注册表游戏路径

                if (gameInfo.GameType is GameType.ITT)
                {
                    // 双人成行
                    startInfo.FileName = Path.Combine(gameInfo.Dir, "Nuts\\Binaries\\Win64", gameInfo.AppName);
                    startInfo.WorkingDirectory = Path.Combine(gameInfo.Dir, "Nuts\\Binaries\\Win64");
                }
                else
                {
                    // 其他
                    startInfo.FileName = Path.Combine(gameInfo.Dir, gameInfo.AppName);
                    startInfo.WorkingDirectory = gameInfo.Dir;
                }

                // 启动参数
                startInfo.Arguments = string.Concat(webArgs, " ", gameInfo.Args).Trim();
            }

            // 批量设置进程启动环境变量
            foreach (var variable in environmentVariables)
            {
                startInfo.EnvironmentVariables[variable.Key] = variable.Value;
            }

            // 启动程序
            Process.Start(startInfo);

            LoggerHelper.Info($"启动游戏 {gameInfo.Name} 成功");
            if (isNotice)
                NotifierHelper.Success($"启动游戏 {gameInfo.Name} 成功");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"启动游戏发生异常 {gameType}", ex);
            if (isNotice)
                NotifierHelper.Error($"启动游戏发生异常 {gameType} 详情请看日志");
        }
    }
}
