using EAappEmulater.Enums;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using System.Runtime.Serialization.Formatters.Binary;

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
            else if (gameInfo.GameType is GameType.SWJFO)
            {
                execPath = Path.Combine(gameInfo.Dir, "SwGame\\Binaries\\Win64", gameInfo.AppName);
                execPath2 = Path.Combine(gameInfo.Dir2, "SwGame\\Binaries\\Win64", gameInfo.AppName);
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
                    LoggerHelper.Warn(I18nHelper.I18n._("Core.Game.StartGameErrorDir", gameType, gameInfo.Dir));
                    if (isNotice)
                        NotifierHelper.Warning(I18nHelper.I18n._("Core.Game.StartGameErrorDir", gameType, ""));

                    return;
                }

                // 判断游戏文件
                if (!File.Exists(execPath2))
                {
                    LoggerHelper.Warn(I18nHelper.I18n._("Core.Game.StartGameErrorExe", gameType, execPath2));
                    if (isNotice)
                        NotifierHelper.Warning(I18nHelper.I18n._("Core.Game.StartGameErrorExe", gameType, ""));

                    return;
                }
            }
            else
            {
                // 注册表游戏路径

                // 判断游戏路径
                if (string.IsNullOrWhiteSpace(gameInfo.Dir))
                {
                    LoggerHelper.Warn(I18nHelper.I18n._("Core.Game.StartGameErrorDir", gameType, gameInfo.Dir));
                    if (isNotice)
                        NotifierHelper.Warning(I18nHelper.I18n._("Core.Game.StartGameErrorDir", gameType, ""));

                    return;
                }

                // 判断游戏文件
                if (!File.Exists(execPath))
                {
                    LoggerHelper.Warn(I18nHelper.I18n._("Core.Game.StartGameErrorExe", gameType, execPath));
                    if (isNotice)
                        NotifierHelper.Warning(I18nHelper.I18n._("Core.Game.StartGameErrorExe", gameType, ""));

                    return;
                }
            }

            ////////////////////////////////////////////////////////

            if (string.IsNullOrWhiteSpace(Account.OriginPCToken))
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Core.Game.StartGameErrorToken", gameType));
                if (isNotice)
                    NotifierHelper.Warning(I18nHelper.I18n._("Core.Game.StartGameErrorToken", gameType));

                return;
            }

            ////////////////////////////////////////////////////////

            // 处理旧的 LSX
            if (gameInfo.IsOldLSX)
                BattlelogHttpServer.BattlelogType = BattlelogType.BFH;
            else
                BattlelogHttpServer.BattlelogType = gameType switch
                {
                    GameType.BF3 => BattlelogType.BF3,
                    GameType.BF4 => BattlelogType.BF4,
                    GameType.BFH => BattlelogType.BFH,
                    _ => BattlelogType.None,
                };

            LoggerHelper.Info(I18nHelper.I18n._("Core.Game.StartGameProcess", gameInfo.Name));
            if (isNotice)
                NotifierHelper.Notice(I18nHelper.I18n._("Core.Game.StartGameProcess", gameInfo.Name));

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
            startInfo.Verb = "";

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
                else if (gameInfo.GameType is GameType.SWJFO)
                {
                    // 星球大战 绝地：陨落的武士团
                    startInfo.FileName = Path.Combine(gameInfo.Dir2, "SwGame\\Binaries\\Win64", gameInfo.AppName);
                    startInfo.WorkingDirectory = Path.Combine(gameInfo.Dir2, "SwGame\\Binaries\\Win64", gameInfo.AppName);
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
                else if (gameInfo.GameType is GameType.SWJFO)
                {
                    // 星球大战 绝地：陨落的武士团
                    startInfo.FileName = Path.Combine(gameInfo.Dir, "SwGame\\Binaries\\Win64", gameInfo.AppName);
                    startInfo.WorkingDirectory = Path.Combine(gameInfo.Dir, "SwGame\\Binaries\\Win64");
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


            string serializedData = $"{startInfo.FileName};{startInfo.WorkingDirectory};{startInfo.Arguments};{Account.OriginPCToken};{Account.PlayerName};{EaCrypto.GetRTPHandshakeCode()};{gameInfo.ContentId}";

            // 启动程序
            using (var pipeClient = new NamedPipeClientStream(".", "RunGame_OriginDebug", PipeDirection.Out))
            {
                pipeClient.Connect();
                using (var writer = new StreamWriter(pipeClient))
                {
                    writer.WriteLine(serializedData);
                }
            }

            //Process.Start(startInfo);

            LoggerHelper.Info(I18nHelper.I18n._("Core.Game.StartGameSuccess", gameInfo.Name));
            if (isNotice)
                NotifierHelper.Success(I18nHelper.I18n._("Core.Game.StartGameSuccess", gameInfo.Name));
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Core.Game.StartGameError", gameType, ex));
            if (isNotice)
                NotifierHelper.Error(I18nHelper.I18n._("Core.Game.StartGameErrorNotice", gameType));
        }
    }
}
