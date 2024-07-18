using NLog.Common;
using NLog.Targets;

namespace EAappEmulater.Extend;

[Target("NlogViewer")]
public class NlogViewerTarget : Target
{
    public event Action<AsyncLogEventInfo> LogReceived;

    protected override void Write(AsyncLogEventInfo logEvent)
    {
        base.Write(logEvent);

        LogReceived?.Invoke(logEvent);
    }
}
