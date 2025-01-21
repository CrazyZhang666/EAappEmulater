using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

namespace OriginDebug;

internal class Program
{
    private static Dictionary<string, string> GetEnvironmentVariables()
    {
        var environmentVariables = new Dictionary<string, string>();
        foreach (DictionaryEntry dirEnity in Environment.GetEnvironmentVariables())
        {
            environmentVariables.Add(dirEnity.Key.ToString(), dirEnity.Value.ToString());
        }
        return environmentVariables;
    }

    static void Main(string[] args)
    {
        while (true)
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("RunGame_OriginDebug", PipeDirection.In))
            {
                pipeServer.WaitForConnection();
                string serializedData = "";
                try
                {
                    using (var reader = new StreamReader(pipeServer))
                    {
                        serializedData = reader.ReadLine();
                    }
                    string[] data = serializedData.Split(';');
                    string fileName = data[0];
                    string WorkingDirectory = data[1];
                    string Arguments = data[2];
                    string OriginPCToken = data[3];
                    string PlayerName = data[4];
                    string EARtPLaunchCode = data[5];
                    string ContentId = data[6];

                    // 获取当前进程所有环境变量名及其值
                    var environmentVariables = GetEnvironmentVariables();
                    environmentVariables["EAFreeTrialGame"] = "false";
                    environmentVariables["EAAuthCode"] = OriginPCToken;
                    environmentVariables["EALaunchOfflineMode"] = "false";
                    environmentVariables["OriginSessionKey"] = "7102090b-ea9a-4531-9598-b2a7e943b544";
                    environmentVariables["EAGameLocale"] = "zh_TW";
                    environmentVariables["EALaunchEnv"] = "production";
                    environmentVariables["EALaunchEAID"] = PlayerName;
                    environmentVariables["EALicenseToken"] = "114514";
                    environmentVariables["EAEntitlementSource"] = "EA";
                    environmentVariables["EAUseIGOAPI"] = "1";
                    environmentVariables["EALaunchUserAuthToken"] = OriginPCToken;
                    environmentVariables["EAGenericAuthToken"] = OriginPCToken;
                    environmentVariables["EALaunchCode"] = "unavailable";
                    environmentVariables["EARtPLaunchCode"] = EARtPLaunchCode;
                    environmentVariables["EALsxPort"] = "3216";
                    environmentVariables["EAEgsProxyIpcPort"] = "1705";
                    environmentVariables["EASteamProxyIpcPort"] = "1704";
                    environmentVariables["EAExternalSource"] = "EA";
                    environmentVariables["EASecureLaunchTokenTemp"] = "1001006949032";
                    environmentVariables["SteamAppId"] = "";
                    environmentVariables["ContentId"] = ContentId;
                    environmentVariables["EAConnectionId"] = ContentId;
                    environmentVariables["OPENSSL_ia32cap"] = "~0x200000200000000";

                    var startInfo = new ProcessStartInfo
                    {
                        UseShellExecute = false
                    };
                    startInfo.Verb = "";
                    startInfo.FileName = fileName;
                    startInfo.WorkingDirectory = WorkingDirectory;
                    startInfo.Arguments = Arguments;
                    foreach (var variable in environmentVariables)
                    {
                        startInfo.EnvironmentVariables[variable.Key] = variable.Value;
                    }
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {

                }
                finally
                {

                }
            }
        }
    }
}
