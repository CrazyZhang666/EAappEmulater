using BF1ModTools.Api;
using BF1ModTools.Helper;

namespace BF1ModTools.Core;

public static class LSXTcpServer
{
    private static TcpListener _tcpServer = null;
    private static bool _isRunning = true;

    private static readonly List<string> ScoketMsgBFV = new();

    static LSXTcpServer()
    {
        // 加载XML字符串
        for (int i = 0; i <= 25; i++)
        {
            var text = FileHelper.GetEmbeddedResourceText($"LSX.BFV.{i:D2}.xml");

            // 头像 \AppData\Local\Origin\AvatarsCache（不清楚为啥不显示）
            text = text.Replace("##AvatarId##", "Avatars40.jpg");

            ScoketMsgBFV.Add(text);
        }

        // 这里结束符必须要加
        ScoketMsgBFV[0] = string.Concat(ScoketMsgBFV[0], "\0");
        ScoketMsgBFV[1] = string.Concat(ScoketMsgBFV[1], "\0");
        ScoketMsgBFV[25] = string.Concat(ScoketMsgBFV[25], "\0");
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

        LoggerHelper.Info("启动 LSX 监听服务成功");
        LoggerHelper.Debug("LSX 服务监听端口为 3216");

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
    /// 获取玩家列表Xml字符串
    /// </summary>
    private static string GetFriendsXmlString()
    {
        return ScoketMsgBFV[11];
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
                LoggerHelper.Debug($"发现 TCP 客户端连接 {client.Client.RemoteEndPoint}");
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
        // 建立和连接的客户端的数据流（传输数据）
        var networkStream = client.GetStream();

        try
        {
            var startKey = "cacf897a20b6d612ad0c05e011df52bb";
            var buffer = Encoding.UTF8.GetBytes(ScoketMsgBFV[0].Replace("##KEY##", startKey));

            // 异步写入网络流
            await networkStream.WriteAsync(buffer);

            var tcpString = await ReadTcpString(networkStream);
            var partArray = tcpString.Split('\"');

            // 适配FC24
            var doc = XDocument.Parse(tcpString.Replace("version=\"\"", "version1=\"\""));
            var request = doc.Element("LSX").Element("Request");
            var contentId = request.Element("ChallengeResponse").Element("ContentId").Value;

            // 重要！！！
            var response = partArray[5];
            var key = partArray[7];

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

            // 处理请求
            buffer = Encoding.UTF8.GetBytes(ScoketMsgBFV[25].Replace("##RESPONSE##", newResponse).Replace("##ID##", partArray[3]));

            // 异步写入网络流
            await networkStream.WriteAsync(buffer);

            while (_isRunning)
            {
                try
                {
                    var data = await ReadTcpString(networkStream);
                    data = EaCrypto.LSXDecryptBF4(data, seed);
                    data = await LSXRequestHandleForBFV(data, contentId);
                    data = EaCrypto.LSXEncryptBF4(data, seed);
                    await WriteTcpString(networkStream, $"{data}\0");
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

        // 缓冲区
        var buffer = new byte[81920];
        // 实际读取的长度
        int readCount;

        try
        {
            while ((readCount = await stream.ReadAsync(buffer)) != 0)
            {
                // 将读取的字节流转化为字符串
                var partStr = Encoding.UTF8.GetString(buffer, 0, readCount);
                // 检查读取的字符串是否有结束符，并得到其位置
                var endIndex = partStr.IndexOf('\0');
                if (endIndex != -1)
                {
                    // 如果找到结束符，追加结束符前面的字符串，然后跳出循环
                    strBuilder.Append(partStr, 0, endIndex);
                    break;
                }
                strBuilder.Append(partStr);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("异步读取网络流 TCP 字符串发生异常", ex);
        }

        var data = strBuilder.ToString();
        LoggerHelper.Debug($"异步读取 TCP 网络流字符串 {data}");

        return data;
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

        LoggerHelper.Debug($"BFV LSX 请求 Id {id}");
        LoggerHelper.Debug($"BFV LSX 请求 RequestType {requestType}");
        LoggerHelper.Debug($"BFV LSX 请求 SettingId {settingId}");

        return requestType switch
        {
            "><GetConfig version=" => ScoketMsgBFV[2].Replace("##ID##", id),
            "><GetAuthCode ClientId=" => ScoketMsgBFV[3].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode(settingId)),
            "><GetAuthCode UserId=" => ScoketMsgBFV[3].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode(partArray[7])),
            "><GetBlockList version=" => ScoketMsgBFV[4].Replace("##ID##", id),
            "><GetGameInfo GameInfoId=" => settingId switch
            {
                "FREETRIAL" => ScoketMsgBFV[19].Replace("##ID##", id),
                "UPTODATE" => ScoketMsgBFV[20].Replace("##ID##", id),
                _ => ScoketMsgBFV[5].Replace("##ID##", id),
            },
            "><GetInternetConnectedState version=" => ScoketMsgBFV[6].Replace("##ID##", id),
            "><GetPresence UserId=" => ScoketMsgBFV[7].Replace("##ID##", id),
            "><GetProfile index=" => ScoketMsgBFV[8].Replace("##ID##", id).Replace("##PID##", "1515810").Replace("##DSNM##", Account.PlayerName),
            "><RequestLicense UserId=" => ScoketMsgBFV[15].Replace("##ID##", id).Replace("##License##", await EasyEaApi.GetLSXLicense(partArray[7], contentid)),
            "><GetSetting SettingId=" => settingId switch
            {
                "ENVIRONMENT" => ScoketMsgBFV[9].Replace("##ID##", id),
                "IS_IGO_AVAILABLE" => ScoketMsgBFV[10].Replace("##ID##", id),
                "IS_IGO_ENABLED" => ScoketMsgBFV[10].Replace("##ID##", id),
                _ => string.Empty,
            },
            "><QueryFriends UserId=" => GetFriendsXmlString().Replace("##ID##", id),
            "><QueryImage ImageId=" => ScoketMsgBFV[12].Replace("##ID##", id).Replace("##ImageId##", settingId).Replace("##Width##", partArray[7]),
            "><QueryPresence UserId=" => ScoketMsgBFV[13].Replace("##ID##", id),
            "><SetPresence UserId=" => ScoketMsgBFV[14].Replace("##ID##", id),
            "><GetAllGameInfo version=" => ScoketMsgBFV[16].Replace("##ID##", id).Replace("##SystemTime##", $"{DateTime.Now:s}"),
            "><IsProgressiveInstallationAvailable ItemId=" => ScoketMsgBFV[17].Replace("##ID##", id).Replace("Origin.OFR.50.0004342", "Origin.OFR.50.0001455"),
            "><QueryContent UserId=" => ScoketMsgBFV[18].Replace("##ID##", id),
            "><QueryEntitlements UserId=" => ScoketMsgBFV[21].Replace("##ID##", id),
            "><QueryOffers UserId=" => ScoketMsgBFV[22].Replace("##ID##", id),
            "><SetDownloaderUtilization Utilization=" => ScoketMsgBFV[23].Replace("##ID##", id),
            "><QueryChunkStatus ItemId=" => ScoketMsgBFV[24].Replace("##ID##", id),
            _ => string.Empty,
        };
    }
}
