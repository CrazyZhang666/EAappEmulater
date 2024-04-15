using EAappEmulater.Api;
using EAappEmulater.Enums;
using EAappEmulater.Helper;

namespace EAappEmulater.Core;

public static class LSXTcpServer
{
    private static TcpListener _tcpServer = null;
    private static bool _isRunning = true;

    private static readonly List<string> StockMSGBFV = new();
    private static readonly List<string> StockMSGBFH = new();
    private static readonly List<string> StockMSGTTF2 = new();

    static LSXTcpServer()
    {
        // 加载XML字符串
        for (int i = 0; i <= 25; i++)
        {
            if (i == 3)
                continue;

            var text = FileHelper.GetEmbeddedResourceText($"LSX.BFV.{i:D2}.xml");

            // 头像（不清楚为啥不显示）
            text = text.Replace("##AvatarId##", "Avatars40.jpg");
            // 当前时间
            text = text.Replace("##SystemTime##", $"{DateTime.Now:s}");

            StockMSGBFV.Add(text);
        }

        StockMSGBFV[0] = string.Concat(StockMSGBFV[0], "\0");
        StockMSGBFV[1] = string.Concat(StockMSGBFV[1], "\0");

        for (int i = 0; i <= 16; i++)
        {
            var text = FileHelper.GetEmbeddedResourceText($"LSX.BFH.{i:D2}.xml");
            StockMSGBFH.Add(text);
        }

        StockMSGBFH[0] = string.Concat(StockMSGBFH[0], "\0");
        StockMSGBFH[1] = string.Concat(StockMSGBFH[1], "\0");

        for (int i = 0; i <= 0; i++)
        {
            var text = FileHelper.GetEmbeddedResourceText($"LSX.TTF2.{i:D2}.xml");

            // 当前时间
            text = text.Replace("##SystemTime##", $"{DateTime.Now:s}");

            StockMSGTTF2.Add(text);
        }
    }

    /// <summary>
    /// 启动 TCP 监听服务
    /// </summary>
    public static void Run()
    {
        if (_tcpServer is not null)
        {
            LoggerHelper.Warn("LSX 监听服务已经在运行，请勿重复启动");
            return;
        }

        _tcpServer = new TcpListener(IPAddress.Parse("127.0.0.1"), 3216);
        _tcpServer.Start();
        LoggerHelper.Info("启动 LSX 监听服务成功，监听端口为 3216");

        // 注意线程释放问题，避免重复创建
        _isRunning = true;
        new Thread(ListenerLocal3216Thread)
        {
            Name = "ListenerLocal3216Thread",
            IsBackground = true
        }.Start();
        LoggerHelper.Info("启动 LSX 监听服务线程成功");
    }

    /// <summary>
    /// 停止 TCP 监听服务
    /// </summary>
    public static void Stop()
    {
        _isRunning = false;
        LoggerHelper.Info("停止 LSX 监听服务线程成功");

        _tcpServer?.Stop();
        _tcpServer = null;
        LoggerHelper.Info("停止 LSX 监听服务成功");
    }

    /// <summary>
    /// 监听本地端口3216线程
    /// </summary>
    private static async void ListenerLocal3216Thread()
    {
        try
        {
            while (_isRunning)
            {
                if (_tcpServer is null)
                    return;

                var client = await _tcpServer.AcceptTcpClientAsync();
                TcpClient3216Handler(client);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("监听 TCP 客户端连接发生异常", ex);
        }
    }

    /// <summary>
    /// 处理本地端口3216客户端连接
    /// </summary>
    private static async void TcpClient3216Handler(TcpClient client)
    {
        var networkStream = client.GetStream();

        try
        {
            var startKey = "cacf897a20b6d612ad0c05e011df52bb";
            var buffer = Encoding.UTF8.GetBytes(StockMSGBFV[0].Replace("##KEY##", startKey));

            // 异步写入网络流
            await networkStream.WriteAsync(buffer);

            var tcpString = await ReadTcpString(networkStream);
            LoggerHelper.Debug($"读取 TCP 字符串 {tcpString}");
            var partArray = tcpString.Split('\"');

            // 适配FC24
            var doc = XDocument.Parse(tcpString.Replace("version=\"\"", "version1=\"\""));
            var request = doc.Element("LSX").Element("Request");
            var contentId = request.Element("ChallengeResponse").Element("ContentId").Value;

            var response = string.Empty;
            var key = string.Empty;

            // 处理 Battlelog 游戏
            // 硬仗和 bf4debug 模式的 lsx 请求不一样
            switch (BattlelogHttpServer.BattlelogType)
            {
                case BattlelogType.BF4:
                    response = partArray[5];
                    key = partArray[7];
                    break;
                case BattlelogType.BFH:
                    response = partArray[7];
                    key = partArray[9];
                    break;
                case BattlelogType.BF4Debug:
                    response = partArray[7];
                    key = partArray[9];
                    break;
                default:
                    response = partArray[5];
                    key = partArray[7];
                    break;
            }

            LoggerHelper.Debug($"本次启动 Challenge Response 为 {response}");
            LoggerHelper.Info($"本次启动 ContentId 为 {contentId}");
            LoggerHelper.Info("准备启动游戏中...");

            // 检查 Challenge 响应
            if (!EaCrypto.CheckChallengeResponse(response, startKey))
            {
                LoggerHelper.Fatal("Challenge Response 致命错误!");
                return;
            }

            // 处理解密 Challenge 响应
            var newResponse = EaCrypto.MakeChallengeResponse(key);
            LoggerHelper.Debug($"处理解密 Challenge 响应 NewResponse {newResponse}");

            var seed = (ushort)((newResponse[0] << 8) | newResponse[1]);
            LoggerHelper.Debug($"处理解密 Challenge 响应 Seed {newResponse}");

            // 处理 战地硬仗 
            buffer = Encoding.UTF8.GetBytes(StockMSGBFH[1].Replace("##RESPONSE##", newResponse).Replace("##ID##", partArray[3]));

            // 异步写入网络流
            await networkStream.WriteAsync(buffer);

            while (_isRunning)
            {
                try
                {
                    switch (BattlelogHttpServer.BattlelogType)
                    {
                        case BattlelogType.BF4:
                            {
                                var data = await ReadTcpString(networkStream);
                                data = EaCrypto.LSXDecryptBF4(data, seed);
                                data = await LSXRequestHandleForBFV(data, contentId);
                                data = EaCrypto.LSXEncryptBF4(data, seed);
                                await WriteTcpString(networkStream, $"{data}\0");
                            }
                            break;
                        case BattlelogType.BFH:
                            {
                                var data = await ReadTcpString(networkStream);
                                data = EaCrypto.LSXDecryptBFH(data, seed);
                                data = await LSXRequestHandleForBFH(data);
                                data = EaCrypto.LSXEncryptBFH(data, seed);
                                await WriteTcpString(networkStream, $"{data}\0");
                            }
                            break;
                        case BattlelogType.BF4Debug:
                            {
                                var data = await ReadTcpString(networkStream);
                                data = EaCrypto.LSXDecryptBFH(data, seed);
                                data = await LSXRequestHandleForBFH(data);
                                data = EaCrypto.LSXEncryptBFH(data, seed);
                                await WriteTcpString(networkStream, $"{data}\0");
                            }
                            break;
                        default:
                            {
                                var data = await ReadTcpString(networkStream);
                                data = EaCrypto.LSXDecryptBF4(data, seed);
                                data = await LSXRequestHandleForBFV(data, contentId);
                                data = EaCrypto.LSXEncryptBF4(data, seed);
                                await WriteTcpString(networkStream, $"{data}\0");
                            }
                            break;
                    }
                }
                catch (TimeoutException ex)
                {
                    LoggerHelper.Error("处理 TCP Battlelog 客户端连接发生异常", ex);
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("处理 TCP 客户端连接发生异常", ex);
        }
        finally
        {
            client.Close();
        }
    }

    /// <summary>
    /// 异步读取 TCP 网络流字符串
    /// </summary>
    private static async Task<string> ReadTcpString(NetworkStream stream)
    {
        var strBuilder = new StringBuilder();

        var buffer = new byte[81920];
        int bytesRead;

        try
        {
            while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
            {
                var part = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var nullIndex = part.IndexOf('\0');
                if (nullIndex != -1)
                {
                    strBuilder.Append(part, 0, nullIndex);
                    break;
                }
                strBuilder.Append(part);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("异步读取网络流 TCP 字符串发生异常", ex);
        }

        return strBuilder.ToString();
    }

    /// <summary>
    /// 异步写入 TCP 网络流字符串
    /// </summary>
    private static async Task WriteTcpString(NetworkStream stream, string tcpStr)
    {
        var buffer = Encoding.UTF8.GetBytes(tcpStr);
        await stream.WriteAsync(buffer);
    }

    /// <summary>
    /// 处理 BFV LSX 请求
    /// </summary>
    private static async Task<string> LSXRequestHandleForBFV(string request, string contentid)
    {
        LoggerHelper.Debug($"BFV LSX 请求 Request {request}");

        if (string.IsNullOrWhiteSpace(request))
            return string.Empty;

        var partArray = request.Split('\"');
        if (partArray.Length < 5)
            return string.Empty;

        var id = partArray[3];
        var requestType = partArray[4];
        var settingId = partArray[5];

        LoggerHelper.Debug($"BFV LSX 请求 RequestType {requestType}");
        LoggerHelper.Debug($"BFV LSX 请求 SettingId {settingId}");

        return requestType switch
        {
            "><GetConfig version=" => StockMSGBFV[2].Replace("##ID##", id),
            "><GetAuthCode ClientId=" => StockMSGBFV[3].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode(settingId)),
            "><GetAuthCode UserId=" => StockMSGBFV[3].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode(partArray[7])),
            "><GetBlockList version=" => StockMSGBFV[4].Replace("##ID##", id),
            "><GetGameInfo GameInfoId=" => settingId switch
            {
                "FREETRIAL" => StockMSGBFV[19].Replace("##ID##", id),
                "UPTODATE" => StockMSGBFV[20].Replace("##ID##", id),
                _ => StockMSGBFV[5].Replace("##ID##", id),
            },
            "><GetInternetConnectedState version=" => StockMSGBFV[6].Replace("##ID##", id),
            "><GetPresence UserId=" => StockMSGBFV[7].Replace("##ID##", id),
            "><GetProfile index=" => StockMSGBFV[8].Replace("##ID##", id).Replace("##PID##", "1515810").Replace("##DSNM##", Account.PlayerName),
            "><RequestLicense UserId=" => StockMSGBFV[15].Replace("##ID##", id).Replace("##License##", await EasyEaApi.GetLSXLicense(partArray[7], contentid)),
            "><GetSetting SettingId=" => settingId switch
            {
                "ENVIRONMENT" => StockMSGBFV[9].Replace("##ID##", id),
                "IS_IGO_AVAILABLE" => StockMSGBFV[10].Replace("##ID##", id),
                "IS_IGO_ENABLED" => StockMSGBFV[10].Replace("##ID##", id),
                _ => string.Empty,
            },
            "><QueryFriends UserId=" => StockMSGBFV[11].Replace("##ID##", id),
            "><QueryImage ImageId=" => StockMSGBFV[12].Replace("##ID##", id).Replace("##ImageId##", settingId).Replace("##Width##", partArray[7]),
            "><QueryPresence UserId=" => StockMSGBFV[13].Replace("##ID##", id),
            "><SetPresence UserId=" => StockMSGBFV[14].Replace("##ID##", id),
            "><GetAllGameInfo version=" => contentid switch
            {
                "1039093" => StockMSGTTF2[0].Replace("##ID##", id),
                _ => StockMSGBFV[16].Replace("##ID##", id),
            },
            "><IsProgressiveInstallationAvailable ItemId=" => StockMSGBFV[17].Replace("##ID##", id).Replace("Origin.OFR.50.0004342", "Origin.OFR.50.0001455"),
            "><QueryContent UserId=" => StockMSGBFV[18].Replace("##ID##", id),
            "><QueryEntitlements UserId=" => StockMSGBFV[21].Replace("##ID##", id),
            "><QueryOffers UserId=" => StockMSGBFV[22].Replace("##ID##", id),
            "><SetDownloaderUtilization Utilization=" => StockMSGBFV[23].Replace("##ID##", id),
            "><QueryChunkStatus ItemId=" => StockMSGBFV[24].Replace("##ID##", id),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// 处理 BFH LSX 请求
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private static async Task<string> LSXRequestHandleForBFH(string request)
    {
        LoggerHelper.Debug($"BFH LSX 请求 Request {request}");

        if (string.IsNullOrWhiteSpace(request))
            return string.Empty;

        var partArray = request.Split('\"');
        if (partArray.Length < 6)
            return string.Empty;

        var id = partArray[3];
        var requestType = partArray[4];
        var settingId = partArray[6];

        LoggerHelper.Debug($"BFH LSX 请求 RequestType {requestType}");
        LoggerHelper.Debug($"BFH LSX 请求 SettingId {settingId}");

        return requestType switch
        {
            "><GetConfig version=" => StockMSGBFH[2].Replace("##ID##", id),
            "><GetAuthCode version=" => StockMSGBFH[3].Replace("##ID##", id).Replace("##AUTHCODE##", await EasyEaApi.GetLSXAutuCode(partArray[7])),
            "><GetBlockList version=" => StockMSGBFH[4].Replace("##ID##", id),
            "><GetGameInfo version=" => StockMSGBFH[5].Replace("##ID##", id),
            "><GetInternetConnectedState version=" => StockMSGBFH[6].Replace("##ID##", id),
            "><GetPresence version=" => StockMSGBFH[7].Replace("##ID##", id),
            "><GetProfile version=" => StockMSGBFH[8].Replace("##ID##", id),
            "><RequestLicense UserId=" => StockMSGBFH[15].Replace("##ID##", id),
            "><GetSetting version=" => settingId switch
            {
                "ENVIRONMENT" => StockMSGBFH[9].Replace("##ID##", id),
                "IS_IGO_AVAILABLE" => StockMSGBFH[10].Replace("##ID##", id),
                " SettingId=" => StockMSGBFH[10].Replace("##ID##", id),
                _ => string.Empty,
            },
            "><QueryFriends UserId=" => StockMSGBFH[11].Replace("##ID##", id),
            "><QueryImage ImageId=" => StockMSGBFH[12].Replace("##ID##", id),
            "><QueryPresence UserId=" => StockMSGBFH[13].Replace("##ID##", id),
            "><SetPresence version=" => StockMSGBFH[14].Replace("##ID##", id),
            "><GetAuthToken version=" => StockMSGBFH[16].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode("GOS-BlazeServer-HAVANA-PC")),
            _ => string.Empty,
        };
    }
}
