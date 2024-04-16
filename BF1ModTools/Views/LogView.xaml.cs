using BF1ModTools.Extend;
using NLog;
using NLog.Common;

namespace BF1ModTools.Views;

/// <summary>
/// LogView.xaml 的交互逻辑
/// </summary>
public partial class LogView : UserControl
{
    private const int _maxRowCount = 100;

    public LogView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            var targetResult = LogManager.Configuration.AllTargets
                .Where(t => t is NlogViewerTarget).Cast<NlogViewerTarget>();

            foreach (var target in targetResult)
            {
                target.LogReceived += LogReceived;
            }
        }
    }

    private void LogReceived(AsyncLogEventInfo logEventInfo)
    {
        var logEvent = logEventInfo.LogEvent;

        this.Dispatcher.BeginInvoke(() =>
        {
            if (_maxRowCount > 0 && TextBox_Logger.LineCount > _maxRowCount)
                TextBox_Logger.Clear();

            // 追加日志
            TextBox_Logger.AppendText($"[{logEvent.TimeStamp:HH:mm:ss}] [{logEvent.Level.Name}] {logEvent.Message} {logEvent.Exception?.Message}{Environment.NewLine}");

            // 滚动最后一行
            TextBox_Logger.ScrollToEnd();
        });
    }
}
