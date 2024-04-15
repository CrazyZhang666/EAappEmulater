namespace EAappEmulater.Api;

public class RespResult
{
    private Stopwatch _stopwatch = null;

    public string ApiName { get; private set; }
    public double ExecTime { get; private set; }

    public bool IsSuccess { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public string Content { get; set; }
    public string Exception { get; set; }

    public RespResult(string apiName)
    {
        ApiName = apiName;
    }

    public void Start()
    {
        if (_stopwatch is not null)
            return;

        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public void Stop()
    {
        if (_stopwatch is null)
            return;

        _stopwatch.Stop();
        ExecTime = _stopwatch.Elapsed.TotalSeconds;
    }
}
