using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

namespace OriginDebug;

internal class Program
{
    #region Native API for ShellExecute

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr ShellExecute(
        IntPtr hwnd,
        string lpOperation,
        string lpFile,
        string lpParameters,
        string lpDirectory,
        int nShowCmd);

    const int SW_HIDE = 0;
    const int SW_SHOWNORMAL = 1;

    #endregion

    /// <summary>
    /// 通过创建bat，让explorer启动并传递环境变量
    /// 游击战、敌后方，铲除伪政权。游击战、敌后方,坚持反扫荡
    /// </summary>
    static void StartWithExplorerAndEnv(
        string fileName,
        string arguments,
        string workingDirectory,
        Dictionary<string, string> envVars)
    {
        string tempBat = Path.Combine(Path.GetTempPath(), $"launch_{Guid.NewGuid():N}.cmd");

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("chcp 65001 >nul");

            //设置环境变量
            foreach (var kv in envVars)
            {
                string value = kv.Value.Replace("%", "%%").Replace("\"", "\"\"");
                sb.AppendLine($"set \"{kv.Key}={value}\"");
            }
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                string drive = Path.GetPathRoot(workingDirectory).TrimEnd('\\');
                sb.AppendLine($"{drive}");
                sb.AppendLine($"cd /d \"{workingDirectory}\"");
            }
            sb.AppendLine($"start \"\" \"{fileName}\" {arguments}");
            sb.AppendLine($"del \"%~f0\"");
            File.WriteAllText(tempBat, sb.ToString(), new UTF8Encoding(false));

            IntPtr result = ShellExecute(
                IntPtr.Zero,
                "open",
                tempBat,
                null,
                null,
                SW_HIDE);

            if ((int)result <= 32)
                throw new Exception($"调用ShellExecute失败，{(int)result}");
        }
        catch
        {
            try { if (File.Exists(tempBat)) File.Delete(tempBat); } catch { }
            throw;
        }
    }

    private static Dictionary<string, string> GetEnvironmentVariables()
    {
        var environmentVariables = new Dictionary<string, string>();
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            environmentVariables[entry.Key.ToString()] = entry.Value.ToString();
        return environmentVariables;
    }

    static void Main(string[] args)
    {
        while (true)
        {
            using var pipeServer = new NamedPipeServerStream("RunGame_OriginDebug", PipeDirection.In);
            pipeServer.WaitForConnection();

            try
            {
                string serializedData;
                using (var reader = new StreamReader(pipeServer))
                    serializedData = reader.ReadLine();

                string[] data = serializedData.Split(';');
                string fileName = data[0];
                string workingDir = data[1];
                string arguments = data[2];
                string originPCToken = data[3];
                string playerName = data[4];
                string eaRtPLaunch = data[5];
                string contentId = data[6];
                string EAGameLocale = data[7];

                var env = GetEnvironmentVariables();
                env["EAFreeTrialGame"] = "false";
                env["EAAuthCode"] = "NeedsAFreshAuthCode";
                env["EALaunchOfflineMode"] = "false";
                env["OriginSessionKey"] = Guid.NewGuid().ToString();
                env["EAGameLocale"] = EAGameLocale;
                env["EALaunchEnv"] = "production";
                env["EALaunchEAID"] = playerName;
                env["EALicenseToken"] = "Origin.OFR.50.0000721";
                env["EAEntitlementSource"] = "EA";
                env["EAUseIGOAPI"] = "1";
                env["EALaunchUserAuthToken"] = originPCToken;
                env["EAGenericAuthToken"] = originPCToken;
                env["EALaunchCode"] = "unavailable";
                env["EARtPLaunchCode"] = eaRtPLaunch;
                env["EALsxPort"] = "3216";
                env["EAEgsProxyIpcPort"] = "1705";
                env["EASteamProxyIpcPort"] = "1704";
                env["EAExternalSource"] = "EA";
                env["EASecureLaunchTokenTemp"] = "1001006949032";
                env["SteamAppId"] = "";
                env["ContentId"] = contentId;
                env["EAConnectionId"] = contentId;
                env["OPENSSL_ia32cap"] = "~0x200000200000000";
                env["EALaunchOwner"] = "EA";
                env["EAAccessTokenJWS"] = originPCToken;

                StartWithExplorerAndEnv(fileName, arguments, workingDir, env);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动失败: {ex.Message}");
            }
        }
    }
}