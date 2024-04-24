using RestSharp;

namespace EAappEmulater.Api;

public class RespResult
{
    public string ApiName { get; private set; }

    public bool IsSuccess { get; set; }
    public ResponseStatus StatusText { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public string Content { get; set; }
    public string Exception { get; set; }

    public RespResult(string apiName)
    {
        ApiName = apiName;
    }
}
