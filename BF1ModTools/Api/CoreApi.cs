using BF1ModTools.Helper;
using RestSharp;

namespace BF1ModTools.Api;

public static class CoreApi
{
    private static readonly RestClient _client;

    static CoreApi()
    {
        var options = new RestClientOptions()
        {
            Timeout = TimeSpan.FromSeconds(20),
            FollowRedirects = false,
            ThrowOnAnyError = false,
            ThrowOnDeserializationError = false
        };

        _client = new RestClient(options);
    }

    /// <summary>
    /// 获取服务器版本信息
    /// </summary>
    public static async Task<Version> GetWebUpdateVersion()
    {
        try
        {
            var request = new RestRequest("http://120.76.47.131:10086/marne/update.txt", Method.Get);

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"GetWebUpdateVersion 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"GetWebUpdateVersion 请求结束，状态码 {response.StatusCode}");

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"GetWebUpdateVersion 请求超时");
                return null;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (Version.TryParse(response.Content, out Version version))
                {
                    LoggerHelper.Info($"获取服务器更新版本号成功 {version}");
                    return version;
                }
            }

            LoggerHelper.Warn($"获取服务器更新版本号失败 {response.Content}");
            return null;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("获取服务器更新版本号发生异常", ex);
            return null;
        }
    }

    /// <summary>
    /// 获取服务器Mod信息
    /// </summary>
    public static async Task<string> GetWebModInfo()
    {
        try
        {
            var request = new RestRequest("http://120.76.47.131:10086/marne/mod.xml", Method.Get);

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"GetWebModInfo 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"GetWebModInfo 请求结束，状态码 {response.StatusCode}");

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"GetWebModInfo 请求超时");
                return string.Empty;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                LoggerHelper.Info("获取服务器 Mod信息 成功");
                return response.Content;
            }

            LoggerHelper.Warn($"获取服务器 Mod信息 失败 {response.Content}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("获取服务器 Mod信息 发生异常", ex);
            return string.Empty;
        }
    }
}
