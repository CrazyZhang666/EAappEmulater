using BF1ModTools.Helper;

namespace BF1ModTools.Core;

public static class LocalUdpClient
{
    private static UdpClient _udpClient;

    private static bool _isRunning = true;

    static LocalUdpClient()
    {
    }

    public static void Run()
    {
        if (_udpClient is not null)
        {
            LoggerHelper.Warn("Local UDP 监听服务已经在运行，请勿重复启动");
            return;
        }

        uint IOC_IN = 0x80000000;
        uint IOC_VENDOR = 0x18000000;
        uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

        var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 56700);

        _udpClient = new UdpClient(localEndPoint);
        _udpClient.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);

        LoggerHelper.Info("启动 Local UDP 监听服务成功");
        LoggerHelper.Debug("Local UDP 服务监听端口为 56700");

        // 注意线程释放问题，避免重复创建
        _isRunning = true;
        new Thread(ListenerLocal56700Thread)
        {
            Name = "ListenerLocal56700Thread",
            IsBackground = true
        }.Start();
        LoggerHelper.Info("启动 Local UDP 监听服务线程成功");
    }

    public static void Stop()
    {
        _isRunning = false;
        LoggerHelper.Info("停止 Local UDP 监听服务线程成功");

        _udpClient?.Close();
        _udpClient = null;
        LoggerHelper.Info("停止 Local UDP 监听服务成功");
    }

    /// <summary>
    /// 监听本地端口56700线程
    /// </summary>
    private static async void ListenerLocal56700Thread()
    {
        try
        {
            while (_isRunning)
            {
                if (_udpClient is null)
                    return;

                var received = await _udpClient.ReceiveAsync();

                if (received.Buffer.Length == 0)
                    continue;

                var content = Encoding.UTF8.GetString(received.Buffer);
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                LoggerHelper.Info($"收到 Local UDP 消息 {content}");

                if (content.Trim() == "RunBf1Game")
                {
                    Game.RunBf1Game();
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("处理 Local UDP 客户端连接发生异常", ex);
        }
    }
}
