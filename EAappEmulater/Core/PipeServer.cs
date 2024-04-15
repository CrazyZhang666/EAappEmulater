using EAappEmulater.Enums;
using EAappEmulater.Helper;

namespace EAappEmulater.Core;

public class PipeServer
{
    private readonly NamedPipeClientStream _pipeClient;
    private readonly BattlelogType _battleType;

    private bool _isRunning = true;

    public string GameState { get; set; } = null;

    public PipeServer(BattlelogType battleType)
    {
        this._battleType = battleType;

        _pipeClient = battleType switch
        {
            BattlelogType.BF3 => new NamedPipeClientStream(".", "venice_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BF4 => new NamedPipeClientStream(".", "warsaw_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BF4Debug => new NamedPipeClientStream(".", "warsaw_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BFH => new NamedPipeClientStream(".", "omaha_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BF1 => new NamedPipeClientStream(".", "tunguska_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BFV => new NamedPipeClientStream(".", "tunguska_snowroller", PipeDirection.InOut, PipeOptions.None),
            _ => new NamedPipeClientStream(".", "venice_snowroller", PipeDirection.InOut, PipeOptions.None),
        };

        new Thread(PipeHandlerThread)
        {
            Name = "PipeHandlerThread",
            IsBackground = true
        }.Start();

        LoggerHelper.Info($"{_battleType} 启动 Pipe 监听服务成功");
    }

    /// <summary>
    /// 销毁 Pipe 监听服务
    /// </summary>
    public void Dispose()
    {
        _isRunning = false;

        LoggerHelper.Info($"{_battleType} 停止 Pipe 监听服务成功");
    }

    private void PipeHandlerThread()
    {
        try
        {
            var array = new byte[256];
            using var binaryReader = new BinaryReader(_pipeClient);

            while (_isRunning)
            {
                try
                {
                    // 等待连接到服务器
                    if (!_pipeClient.IsConnected)
                    {
                        // 超时时长 3600 秒
                        _pipeClient.Connect(3600000);
                    }
                }
                catch (TimeoutException ex)
                {
                    LoggerHelper.Error($"{_battleType} 处理 Pipe 客户端连接发生异常", ex);
                    return;
                }

                // 处理已连接 PipeStream 对象
                while (binaryReader.Read(array, 0, array.Length) != 0)
                {
                    var stringBuilder = new StringBuilder();
                    var memoryStream = new MemoryStream();

                    var buffer = array[4];

                    memoryStream.Write(array, 5, buffer);
                    stringBuilder.Append(Encoding.ASCII.GetString(memoryStream.ToArray()));

                    memoryStream = new MemoryStream();

                    memoryStream.Write(array, 7 + buffer, array[5 + buffer]);
                    stringBuilder.Append(" : " + Encoding.ASCII.GetString(memoryStream.ToArray()));

                    GameState = stringBuilder.ToString();
                    LoggerHelper.Debug($"{_battleType} Pipe 当前游戏状态 {stringBuilder}");
                }

                Thread.Sleep(1000);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"{_battleType} 处理 Pipe 客户端连接发生异常", ex);
        }
    }
}