using BF1ModTools.Core;
using BF1ModTools.Helper;
using RestSharp;

namespace BF1ModTools.Api;

public static class EaApi
{
    private static readonly RestClient _client;

    static EaApi()
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

    /// <summary>
    /// Api 请求成功后更新 cookie
    /// </summary>
    private static void UpdateCookie(CookieCollection cookies, string apiName)
    {
        LoggerHelper.Info($"{apiName} Cookie 数量为 {cookies.Count}");

        foreach (var item in cookies.ToList())
        {
            if (item.Name.Equals("remid", StringComparison.OrdinalIgnoreCase))
            {
                Account.Remid = item.Value;
                LoggerHelper.Info($"{apiName} 获取 Remid 成功 {Account.Remid}");
                continue;
            }

            if (item.Name.Equals("sid", StringComparison.OrdinalIgnoreCase))
            {
                Account.Sid = item.Value;
                LoggerHelper.Info($"{apiName} 获取 Sid 成功 {Account.Sid}");
                continue;
            }
        }
    }

    /// <summary>
    /// 通过玩家 cookie 获取 token (结果 access_token)
    /// </summary>
    public static async Task<RespResult> GetToken()
    {
        var respResult = new RespResult("GetToken Api");

        if (string.IsNullOrWhiteSpace(Account.Remid) || string.IsNullOrWhiteSpace(Account.Sid))
        {
            LoggerHelper.Warn($"Remid 或 Sid 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        try
        {
            var request = new RestRequest("https://accounts.ea.com/connect/auth")
            {
                Method = Method.Get
            };

            request.AddParameter("client_id", "ORIGIN_JS_SDK");
            request.AddParameter("response_type", "token");
            request.AddParameter("redirect_uri", "nucleus:rest");
            request.AddParameter("prompt", "none");
            request.AddParameter("release_type", "prod");

            request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.193 Safari/537.36");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Cookie", $"remid={Account.Remid};sid={Account.Sid};");

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"{respResult.ApiName} 请求完成，状态码 {response.StatusCode}");

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                // 错误返回 {"error_code":"login_required","error":"login_required","error_number":"102100"}

                var content = JsonHelper.JsonDeserialize<Token>(response.Content);
                Account.AccessToken = content.access_token;
                LoggerHelper.Info($"{respResult.ApiName} 获取 AccessToken 成功 {Account.AccessToken}");

                respResult.IsSuccess = true;

                UpdateCookie(response.Cookies, respResult.ApiName);
            }
            else
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求失败，返回结果 {response.Content}");
            }
        }
        catch (Exception ex)
        {
            respResult.Exception = ex.Message;
            LoggerHelper.Error($"{respResult.ApiName} 请求异常", ex);
        }

        return respResult;
    }
}
