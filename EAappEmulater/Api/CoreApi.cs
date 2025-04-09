using EAappEmulater.Helper;
using RestSharp;
using System;

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
            var request = new RestRequest("https://api.github.com/repos/CrazyZhang666/EAappEmulater/releases/latest", Method.Get);

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info(I18nHelper.I18n._("Api.CoreApi.GetWebUpdateVersionStatus", response.ResponseStatus));
            LoggerHelper.Info(I18nHelper.I18n._("Api.CoreApi.GetWebUpdateVersionStatus", response.StatusCode));

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Api.CoreApi.GetWebUpdateVersionTimeout"));
                return null;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var jsonNode = JsonNode.Parse(response.Content);

                var tagName = jsonNode["tag_name"].GetValue<string>();
                if (Version.TryParse(tagName, out Version version))
                {
                    LoggerHelper.Info(I18nHelper.I18n._("Api.CoreApi.GetWebUpdateVersionSuccess", version));
                    return version;
                }
            }

            LoggerHelper.Warn(I18nHelper.I18n._("Api.CoreApi.GetWebUpdateVersionError", response.Content));
            return null;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Api.CoreApi.GetWebUpdateVersionErrorEx", ex));
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
                LoggerHelper.Warn(I18nHelper.I18n._("Api.CoreApi.DownloadWebImageError", imgUrl));
                return false;
            }

            await File.WriteAllBytesAsync(savePath, bytes);
            return true;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Api.CoreApi.DownloadWebImageErrorEx", imgUrl, ex));
            return false;
        }
    }
}
