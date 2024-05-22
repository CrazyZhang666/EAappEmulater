using BF1ModTools.Helper;

namespace BF1ModTools.Core;

public static class LocalHttpServer
{
    private static HttpListener _httpListener;

    static LocalHttpServer()
    {
    }

    public static void Run()
    {
        if (_httpListener is not null)
        {
            LoggerHelper.Warn("Local HTTP 监听服务已经在运行，请勿重复启动");
            return;
        }

        _httpListener = new HttpListener
        {
            AuthenticationSchemes = AuthenticationSchemes.Anonymous
        };
        _httpListener.Prefixes.Add("http://127.0.0.1:59743/");
        _httpListener.Start();
        _httpListener.BeginGetContext(Result, null);

        LoggerHelper.Info("启动 Local Http 监听服务成功");
        LoggerHelper.Debug("Local Http 服务监听端口为 59743");
    }

    public static void Stop()
    {
        _httpListener?.Stop();
        _httpListener?.Close();
        _httpListener = null;
        LoggerHelper.Info("停止 Local Http 监听服务成功");
    }

    /// <summary>
    /// 监听本地Http端口59743线程
    /// </summary>
    private static void Result(IAsyncResult asyncResult)
    {
        try
        {
            if (_httpListener is null)
                return;

            // 获取当前连接对象
            var context = _httpListener.EndGetContext(asyncResult);
            var request = context.Request;
            var response = context.Response;

            // 继续异步监听
            _httpListener.BeginGetContext(Result, null);

            if (request.HttpMethod == "GET")
            {
                LoggerHelper.Info($"收到 Local HTTP GET请求 {request.Url}");

                if (request.RawUrl.Equals("/RunBf1Game", StringComparison.OrdinalIgnoreCase))
                {
                    // 不加这个 Notification.Wpf 会导致崩溃
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Game.RunBf1Game();
                    });

                    response.OutputStream.Write(Encoding.UTF8.GetBytes("启动战地1游戏成功"));
                    response.StatusCode = 200;
                    response.Close();
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("处理 Local HTTP 请求发生异常", ex);
        }
    }
}
