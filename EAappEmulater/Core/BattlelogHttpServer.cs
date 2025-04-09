using EAappEmulater.Enums;
using EAappEmulater.Helper;

namespace EAappEmulater.Core;

public static class BattlelogHttpServer
{
    private static HttpListener _httpListener = null;

    private static readonly Dictionary<string, string> _gameStatus = new();

    public static BattlelogType BattlelogType { get; set; }

    private static PipeServer _bf3PipeServer = null;
    private static PipeServer _bf4PipeServer = null;
    private static PipeServer _bfHPipeServer = null;

    private static bool _isBf3BattlelogGameStart = false;
    private static bool _isBf4BattlelogGameStart = false;
    private static bool _isBfHBattlelogGameStart = false;

    static BattlelogHttpServer()
    {
        BattlelogType = BattlelogType.None;

        _gameStatus["50182"] = FileHelper.GetEmbeddedResourceText($"Battlelog.50182.json");
        _gameStatus["000000"] = FileHelper.GetEmbeddedResourceText($"Battlelog.000000.json");
        _gameStatus["ping"] = FileHelper.GetEmbeddedResourceText($"Battlelog.ping.json");
        _gameStatus["2f4c24"] = FileHelper.GetEmbeddedResourceText($"Battlelog.2f4c24.json");
        _gameStatus["181931"] = FileHelper.GetEmbeddedResourceText($"Battlelog.181931.json");
        _gameStatus["76889"] = FileHelper.GetEmbeddedResourceText($"Battlelog.76889.json");
        _gameStatus["182288"] = FileHelper.GetEmbeddedResourceText($"Battlelog.182288.json");

        _gameStatus["status"] = FileHelper.GetEmbeddedResourceText($"Battlelog.status.json");
    }

    /// <summary>
    /// 启动 Battlelog 监听服务
    /// </summary>
    public static void Run()
    {
        if (_httpListener is not null)
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Core.BattlelogHttpServer.AlreadyListen"));
            return;
        }

        _httpListener = new HttpListener
        {
            AuthenticationSchemes = AuthenticationSchemes.Anonymous
        };

        _httpListener.Prefixes.Add("http://127.0.0.1:3215/");
        _httpListener.Prefixes.Add("http://127.0.0.1:4219/");
        _httpListener.Start();

        LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.ListenSuccess"));
        LoggerHelper.Debug(I18nHelper.I18n._("Core.BattlelogHttpServer.ListenSuccessDebug"));

        _httpListener.BeginGetContext(Result, null);

        /////////////////////////////////////////////////

        _bf3PipeServer = new PipeServer(BattlelogType.BF3);
        _bf4PipeServer = new PipeServer(BattlelogType.BF4);
        _bfHPipeServer = new PipeServer(BattlelogType.BFH);
    }

    /// <summary>
    /// 停止 Battlelog 监听服务
    /// </summary>
    public static void Stop()
    {
        _httpListener?.Stop();
        _httpListener = null;
        LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.StopListenSuccess"));

        /////////////////////////////////////////////////

        _bf3PipeServer?.Dispose();
        _bf3PipeServer = null;

        _bf4PipeServer?.Dispose();
        _bf4PipeServer = null;

        _bfHPipeServer?.Dispose();
        _bfHPipeServer = null;
    }

    /// <summary>
    /// 通用响应输出流写入
    /// </summary>
    private static void WriteOutputStream(HttpListenerContext context, int code, bool isNeedHeader, string text)
    {
        context.Response.StatusCode = code;

        if (isNeedHeader)
            context.Response.Headers.Add("Access-Control-Allow-Origin", "https://battlelog.battlefield.com");

        var bytes = Encoding.UTF8.GetBytes(text);
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);

        context.Response.Close();
    }

    /// <summary>
    /// 处理传入的请求
    /// </summary>
    private static void Result(IAsyncResult asyncResult)
    {
        try
        {
            // 避免关闭时抛出异常
            if (_httpListener is null)
                return;

            // 完成检索传入的客户端请求的异步操作
            var context = _httpListener.EndGetContext(asyncResult);
            // 开始异步检索传入的请求（下一个请求）
            _httpListener.BeginGetContext(Result, null);

            // 处理 GET 请求
            if (context.Request.HttpMethod == "GET")
            {
                if (context.Request.RawUrl != "/")
                    LoggerHelper.Debug(I18nHelper.I18n._("Core.BattlelogHttpServer.GetUrlDebug", context.Request.Url));

                // 处理 4219 端口请求
                if (context.Request.UserHostName == "127.0.0.1:4219")
                {
                    if (context.Request.RawUrl == "/killgame")
                    {
                        WriteOutputStream(context, 200, true, "");
                    }
                    else
                    {
                        switch (BattlelogType)
                        {
                            case BattlelogType.BF3:
                                {
                                    if (_bf3PipeServer.GameState == null)
                                    {
                                        // Pipe 管道服务 无游戏状态
                                        WriteOutputStream(context, 502, false, "null");
                                    }
                                    else if (_bf3PipeServer.GameState != null && ProcessHelper.IsAppRun("bf3") || ProcessHelper.IsAppRun("bf3debug"))
                                    {
                                        // Pipe 管道服务 有游戏状态，bf3.exe 或 bf3debug.exe 已运行
                                        var newState = _bf3PipeServer.GameState.Replace(" : S", "\tS").Replace(" ", "\t").Replace(":\tERR", "\tERR");
                                        WriteOutputStream(context, 200, true, $"VENICE-GAME\t{newState}");

                                        _isBf3BattlelogGameStart = true;
                                    }
                                    else if (!ProcessHelper.IsAppRun("bf3") || !ProcessHelper.IsAppRun("bf3debug") && _isBf3BattlelogGameStart == true)
                                    {
                                        // bf3.exe 或 bf3debug.exe 未运行，Battlelog Game 已启动
                                        WriteOutputStream(context, 200, true, "VENICE-GAME\tStateChanged\tGAMEISGONE");

                                        BattlelogType = BattlelogType.None;
                                        _isBf3BattlelogGameStart = false;
                                        _bf3PipeServer.GameState = null;
                                    }
                                }
                                break;
                            case BattlelogType.BF4:
                                {
                                    if (_bf4PipeServer.GameState == null)
                                    {
                                        WriteOutputStream(context, 502, false, "null");
                                    }
                                    else if (_bf4PipeServer.GameState != null && ProcessHelper.IsAppRun("bf4") || ProcessHelper.IsAppRun("bf4debug"))
                                    {
                                        var newState = _bf4PipeServer.GameState.Replace(" : S", "\tS").Replace(" ", "\t").Replace(":\tERR", "\tERR");
                                        WriteOutputStream(context, 200, true, $"WARSAW-GAME\t{newState}");

                                        _isBf4BattlelogGameStart = true;
                                    }
                                    else if (!ProcessHelper.IsAppRun("bf4") || !ProcessHelper.IsAppRun("bf4debug") && _isBf4BattlelogGameStart == true)
                                    {
                                        WriteOutputStream(context, 200, true, "WARSAW-GAME\tStateChanged\tGAMEISGONE");

                                        BattlelogType = BattlelogType.None;
                                        _isBf4BattlelogGameStart = false;
                                        _bf4PipeServer.GameState = null;
                                    }
                                }
                                break;
                            case BattlelogType.BFH:
                                {
                                    if (_bfHPipeServer.GameState == null)
                                    {
                                        WriteOutputStream(context, 502, false, "null");
                                    }
                                    else if (_bfHPipeServer.GameState != null && ProcessHelper.IsAppRun("bfh") || ProcessHelper.IsAppRun("bfhdebug"))
                                    {
                                        var newState = _bfHPipeServer.GameState.Replace(" : S", "\tS").Replace(" ", "\t").Replace(":\tERR", "\tERR");
                                        WriteOutputStream(context, 200, true, $"OMAHA-MAINLINE-GAME\t{newState}");

                                        _isBfHBattlelogGameStart = true;
                                    }
                                    else if (!ProcessHelper.IsAppRun("bfh") || !ProcessHelper.IsAppRun("bfhdebug") && _isBfHBattlelogGameStart == true)
                                    {
                                        WriteOutputStream(context, 200, true, "OMAHA-MAINLINE-GAME\tStateChanged\tGAMEISGONE");

                                        BattlelogType = BattlelogType.None;
                                        _isBfHBattlelogGameStart = false;
                                        _bfHPipeServer.GameState = null;
                                    }
                                }
                                break;
                        }
                    }
                }
                else
                {
                    switch (context.Request.RawUrl)
                    {
                        case "/game/status?masterTitleId=50182":
                            LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.Get50182"));
                            WriteOutputStream(context, 200, true, _gameStatus["50182"]);
                            break;
                        case "/game/status?masterTitleId=000000":
                            WriteOutputStream(context, 404, true, _gameStatus["000000"]);
                            break;
                        case "/ping":
                            WriteOutputStream(context, 200, true, _gameStatus["ping"]);
                            break;
                        case "/game/launch/status/cee4e0c885634dc2bfcb7ee88e2f4c24":
                            LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.Get2f4c24"));
                            WriteOutputStream(context, 200, true, _gameStatus["2f4c24"]);
                            break;
                        case "/game/status?masterTitleId=181931":
                            LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.Get181931"));
                            WriteOutputStream(context, 200, true, _gameStatus["181931"]);
                            break;
                        case "/game/status?masterTitleId=76889":
                            LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.Get76889"));
                            WriteOutputStream(context, 200, true, _gameStatus["76889"]);
                            break;
                        case "/game/status?masterTitleId=182288":
                            LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.Get182288"));
                            WriteOutputStream(context, 200, true, _gameStatus["182288"]);
                            break;
                        default:
                            context.Response.StatusCode = 200;
                            context.Response.Close();
                            break;
                    }
                }

                // 避免if-else嵌套过多
                return;
            }

            // 处理 POST 请求
            if (context.Request.HttpMethod == "POST")
            {
                if (context.Request.RawUrl != "/")
                    LoggerHelper.Debug(I18nHelper.I18n._("Core.BattlelogHttpServer.PostUrlDebug", context.Request.Url));

                var nameValCol = context.Request.QueryString;
                var cmdParams = nameValCol["cmdParams"];
                var ipAddrList = nameValCol["ipAddrList"];

                if (!string.IsNullOrWhiteSpace(ipAddrList))
                {
                    var ipArray = ipAddrList.Split(',');

                    var strBuilder = new StringBuilder();
                    foreach (var ip in ipArray)
                    {
                        try
                        {
                            Ping ping = new Ping();
                            PingReply reply = ping.Send(ip.Trim());
                            if (reply.Status == IPStatus.Success)
                            {
                                strBuilder.Append($"{{\"ip\":\"{ip}\",\"time\":{reply.RoundtripTime}}},");
                            }
                            else
                            {
                                strBuilder.Append($"{{\"ip\":\"{ip}\",\"time\":-1}},");
                            }
                        }
                        catch (Exception ex)
                        {
                            strBuilder.Append($"{{\"ip\":\"{ip}\",\"time\":-1}},");
                        }
                    }

                    LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.PingBack"));
                    var responseStr = $"[{strBuilder.ToString().TrimEnd(',')}]";
                    WriteOutputStream(context, 200, true, responseStr);
                }
                else if (nameValCol["offerIds"] == "DR:224766400")
                {
                    LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.StartBF3"));
                    BattlelogType = BattlelogType.BF3;

                    Game.RunGame(GameType.BF3, cmdParams, false);
                    WriteOutputStream(context, 202, true, _gameStatus["status"]);
                }
                else if (nameValCol["offerIds"] == "OFB-EAST:109552316@subscription")
                {
                    LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.StartBF4"));

                    Game.RunGame(GameType.BF4, cmdParams, false);
                    WriteOutputStream(context, 202, true, _gameStatus["status"]);
                }
                else if (nameValCol["offerIds"] == "Origin.OFR.50.0000846@subscription")
                {
                    LoggerHelper.Info(I18nHelper.I18n._("Core.BattlelogHttpServer.StartBFHD"));

                    Game.RunGame(GameType.BFH, cmdParams, false);
                    WriteOutputStream(context, 202, true, _gameStatus["status"]);
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Core.BattlelogHttpServer.ClientError", ex));
        }
    }
}
