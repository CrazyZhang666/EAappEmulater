using EAappEmulater.Helper;
using RestSharp;

namespace EAappEmulater.Api;

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

    public static async Task<Version> GetWebUpdateVersion()
    {
        try
        {
            var request = new RestRequest("https://api.battlefield.vip/eaapp/update.txt", Method.Get);

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
    /// 下载网络图片
    /// </summary>
    public static async Task<bool> DownloadWebImage(string imgUrl, string savePath)
    {
        try
        {
            var request = new RestRequest(imgUrl, Method.Get);

            var bytes = await _client.DownloadDataAsync(request);
            if (bytes == null || bytes.Length == 0)
            {
                LoggerHelper.Warn($"下载网络图片失败 {imgUrl}");
                return false;
            }

            await File.WriteAllBytesAsync(savePath, bytes);
            return true;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"下载网络图片发生异常 {imgUrl}", ex);
            return false;
        }
    }
}
