﻿using BF1ModTools.Helper;
using RestSharp;

namespace BF1ModTools.Api;

public static class CoreApi
{
    private static readonly RestClient _client;

    static CoreApi()
    {
        var options = new RestClientOptions()
        {
            MaxTimeout = 9000,
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
            var request = new RestRequest("https://api.battlefield.vip/marne/update.txt", Method.Get);

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"GetWebUpdateVersion 请求完成，状态码 {response.StatusCode}");

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
}
