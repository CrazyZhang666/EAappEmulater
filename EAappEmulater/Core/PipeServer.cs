using EAappEmulater.Enums;
using EAappEmulater.Helper;
using System.Text;

namespace EAappEmulater.Core;

public class PipeServer
{
    private readonly NamedPipeClientStream _pipeClient;
    private readonly BattlelogType _battleType;

    private bool _isRunning = true;
    private readonly Thread _thread;

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

        _thread = new Thread(PipeHandlerThread)
        {
            Name = "PipeHandlerThread",
            IsBackground = true
        };
        _thread.Start();

        
        LoggerHelper.Info(I18nHelper.I18n._("Core.PipeServer.Status", _battleType, _thread.ThreadState));
        LoggerHelper.Info(I18nHelper.I18n._("Core.PipeServer.ListenSuccess", _battleType));
    }

    /// <summary>
    /// 销毁 Pipe 监听服务
    /// </summary>
    public void Dispose()
    {
        _isRunning = false;
        _pipeClient.Close();

        LoggerHelper.Info(I18nHelper.I18n._("Core.PipeServer.Status", _battleType, _thread.ThreadState));
        LoggerHelper.Info(I18nHelper.I18n._("Core.PipeServer.StopListenSuccess", _battleType));
    }

    /// <summary>
    /// Pipe管道处理线程
    /// </summary>
    private async void PipeHandlerThread()
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
                        await _pipeClient.ConnectAsync(3600000);
                    }
                }
                catch (TimeoutException ex)
                {
                    LoggerHelper.Error(I18nHelper.I18n._("Core.PipeServer.ConnErrorTimeOut", _pipeClient, ex));
                    continue;
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
                    LoggerHelper.Debug(I18nHelper.I18n._("Core.PipeServer.GameStatus", _pipeClient, stringBuilder));
                }

                Thread.Sleep(1000);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Core.PipeServer.ConnError", _pipeClient, ex));
        }
    }
}