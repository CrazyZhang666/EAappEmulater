using EAappEmulater.Core;
using EAappEmulater.Helper;
using RestSharp;
using System;
using System.Data.SqlTypes;
using System.Security.Policy;

namespace EAappEmulater.Api;

public static class EaApi
{
    private static readonly RestClient _client;

    static EaApi()
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
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态码 {response.StatusCode}");

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求超时");
                return respResult;
            }

            if (response.Content.Contains("error_code", StringComparison.OrdinalIgnoreCase))
            {
                LoggerHelper.Warn($"{respResult.ApiName} 请求失败，Cookie 已失效，返回结果 {response.Content}");
                return respResult;
            }

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

    /// <summary>
    /// 获取登录账号信息 (access_token)
    /// </summary>
    public static async Task<RespResult> GetIdentityMe()
    {
        var respResult = new RespResult("GetIdentityMe Api");

        if (string.IsNullOrWhiteSpace(Account.AccessToken))
        {
            LoggerHelper.Warn($"AccessToken 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        try
        {
            var request = new RestRequest("https://gateway.ea.com/proxy/identity/pids/me/personas")
            {
                Method = Method.Get
            };

            request.AddHeader("X-Expand-Results", "true");
            request.AddHeader("Authorization", $"Bearer {Account.AccessToken}");

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态码 {response.StatusCode}");

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求超时");
                return respResult;
            }

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                respResult.IsSuccess = true;
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

    /// <summary>
    /// 批量获取玩家头像 (access_token)
    /// </summary>
    public static async Task<RespResult> GetAvatarByUserIds(List<string> userIds)
    {
        var respResult = new RespResult("GetAvatarByUserIds Api");

        if (string.IsNullOrWhiteSpace(Account.AccessToken))
        {
            LoggerHelper.Warn($"AccessToken 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        if (userIds.Count == 0)
        {
            LoggerHelper.Warn($"UserId列表 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        try
        {
            var idStr = string.Join(";", userIds);
            var request = new RestRequest($"https://api1.origin.com/avatar/user/{idStr}/avatars")
            {
                Method = Method.Get
            };

            request.AddParameter("size", "1");

            request.AddHeader("Accept", "application/json");
            request.AddHeader("AuthToken", Account.AccessToken);

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态码 {response.StatusCode}");

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求超时");
                return respResult;
            }

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                respResult.IsSuccess = true;
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

    /// <summary>
    /// 获取登录玩家好友列表 (access_token)
    /// </summary>
    public static async Task<RespResult> GetUserFriends()
    {
        var respResult = new RespResult("GetUserFriends Api");

        if (string.IsNullOrWhiteSpace(Account.AccessToken))
        {
            LoggerHelper.Warn($"AccessToken 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        if (string.IsNullOrWhiteSpace(Account.UserId))
        {
            LoggerHelper.Warn($"UserId 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        try
        {
            var request = new RestRequest($"https://friends.gs.ea.com/friends/2/users/{Account.UserId}/friends")
            {
                Method = Method.Get
            };

            request.AddParameter("count", "250");
            request.AddParameter("names", "true");

            request.AddHeader("X-Api-Version", "2");
            request.AddHeader("X-Application-Key", "origin");
            request.AddHeader("X-AuthToken", Account.AccessToken);

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态码 {response.StatusCode}");

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求超时");
                return respResult;
            }

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                respResult.IsSuccess = true;
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

    /// <summary>
    /// 获取 Origin PC AuthCode (http://127.0.0.1/success?code=????)
    /// 需要 AccessToken 和 cookie
    /// 为 GetOriginPCToken 提供前置参数
    /// </summary>
    public static async Task<RespResult> GetOriginPCAuth()
    {
        var respResult = new RespResult("GetOriginPCAuth Api");

        if (string.IsNullOrWhiteSpace(Account.Remid) || string.IsNullOrWhiteSpace(Account.Sid))
        {
            LoggerHelper.Warn($"Remid 或 Sid 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        if (string.IsNullOrWhiteSpace(Account.AccessToken))
        {
            LoggerHelper.Warn($"AccessToken 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        try
        {
            var request = new RestRequest("https://accounts.ea.com/connect/auth")
            {
                Method = Method.Get
            };

            request.AddParameter("access_token", Account.AccessToken);
            request.AddParameter("client_id", "ORIGIN_PC");
            request.AddParameter("response_type", "code");

            request.AddHeader("User-Agent", "Mozilla / 5.0 EA Download Manager Origin/ 10.5.94.46774");
            request.AddHeader("X-Origin-Platform", "PCWIN");
            request.AddHeader("localeInfo", "zh_TW");
            request.AddHeader("Cookie", $"remid={Account.Remid};sid={Account.Sid};");

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态码 {response.StatusCode}");

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求超时");
                return respResult;
            }

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                var localtion = response.Headers.ToList()
                    .Find(x => x.Name.Equals("location", StringComparison.OrdinalIgnoreCase))
                    .Value.ToString();

                LoggerHelper.Info($"{respResult.ApiName} 获取 localtion 为 {localtion}");
                if (localtion is not null)
                {
                    Account.OriginPCAuth = localtion.Split("=")[1];
                    LoggerHelper.Info($"{respResult.ApiName} 获取 OriginPCAuth 成功 {Account.OriginPCAuth}");

                    respResult.IsSuccess = true;

                    UpdateCookie(response.Cookies, respResult.ApiName);
                }
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

    /// <summary>
    /// 获取 Origin PC AccessToken
    /// 需要 OriginPCAuth 和 cookie
    /// 为 GetAutuCode 提供前置参数
    /// </summary>
    public static async Task<RespResult> GetOriginPCToken()
    {
        var respResult = new RespResult("GetOriginPCToken Api");

        if (string.IsNullOrWhiteSpace(Account.Remid) || string.IsNullOrWhiteSpace(Account.Sid))
        {
            LoggerHelper.Warn($"Remid 或 Sid 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        if (string.IsNullOrWhiteSpace(Account.OriginPCAuth))
        {
            LoggerHelper.Warn($"OriginPCAuth 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        try
        {
            var request = new RestRequest("https://accounts.ea.com/connect/token")
            {
                Method = Method.Post
            };

            request.AddHeader("User-Agent", "Mozilla/5.0 EA Download Manager Origin/10.5.115.51547");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("X-Origin-Platform", "PCWIN");
            request.AddHeader("localeInfo", "en_US");
            request.AddHeader("Cookie", $"remid={Account.Remid};sid={Account.Sid};");

            request.AddParameter("application/x-www-form-urlencoded",
                $"grant_type=authorization_code&code={Account.OriginPCAuth}&client_id=ORIGIN_PC&client_secret=UIY8dwqhi786T78ya8Kna78akjcp0s&redirect_uri=qrc:///html/login_successful.html",
                ParameterType.RequestBody);

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态码 {response.StatusCode}");

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求超时");
                return respResult;
            }

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonHelper.JsonDeserialize<OriginPCToken>(response.Content);
                Account.OriginPCToken = content.access_token;
                LoggerHelper.Info($"{respResult.ApiName} 获取 OriginPCToken 成功 {Account.OriginPCToken}");

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

    /// <summary>
    /// 前置条件
    /// 1. GetToken
    /// 2. GetOriginPCAuth
    /// 3. GetOriginPCToken
    /// 获取LSX游戏许可证
    /// </summary>
    public static async Task<RespResult> GetLSXLicense(string requestToken, string contentId)
    {
        var respResult = new RespResult("GetLSXLicense Api");

        if (string.IsNullOrWhiteSpace(Account.Remid) || string.IsNullOrWhiteSpace(Account.Sid))
        {
            LoggerHelper.Warn($"Remid 或 Sid 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        if (string.IsNullOrWhiteSpace(Account.OriginPCToken))
        {
            LoggerHelper.Warn($"OriginPCToken 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        try
        {
            var request = new RestRequest("https://proxy.novafusion.ea.com/licenses")
            {
                Method = Method.Get
            };

            request.AddParameter("ea_eadmtoken", Account.OriginPCToken);
            request.AddParameter("requestToken", requestToken);
            request.AddParameter("contentId", contentId);
            request.AddParameter("machineHash", "1");
            request.AddParameter("requestType", "0");

            request.AddHeader("User-Agent", "EACTransaction");
            request.AddHeader("X-Requester-Id", "Origin Online Activation");
            request.AddHeader("Cookie", $"remid={Account.Remid};sid={Account.Sid};");

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态码 {response.StatusCode}");

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求超时");
                return respResult;
            }

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var decryptStr = EaCrypto.Decrypt(response.RawBytes).Replace("", "");
                var decryptArray = decryptStr.Split(new string[] { "<GameToken>", "</GameToken>" }, StringSplitOptions.RemoveEmptyEntries);

                if (!string.IsNullOrWhiteSpace(decryptArray[1]))
                {
                    respResult.Content = decryptArray[1];
                    LoggerHelper.Debug($"{respResult.ApiName} 获取 License 成功 {decryptArray[1]}");

                    respResult.IsSuccess = true;

                    UpdateCookie(response.Cookies, respResult.ApiName);
                }
                else
                {
                    LoggerHelper.Warn($"{respResult.ApiName} 获取 License 失败");
                }
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

    /// <summary>
    /// 前置条件
    /// 1. GetToken
    /// 2. GetOriginPCAuth
    /// 3. GetOriginPCToken
    /// 通过 cookie 获取 AutuCode (需要 settingId 作为 client_id 参数)
    /// 特殊版本，和网页登录账号获取 AutuCode 不同
    /// </summary>
    public static async Task<RespResult> GetLSXAutuCode(string settingId)
    {
        var respResult = new RespResult("GetLSXAutuCode Api");

        if (string.IsNullOrWhiteSpace(Account.Remid) || string.IsNullOrWhiteSpace(Account.Sid))
        {
            LoggerHelper.Warn($"Remid 或 Sid 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        if (string.IsNullOrWhiteSpace(Account.OriginPCToken))
        {
            LoggerHelper.Warn($"OriginPCToken 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }

        try
        {
            var request = new RestRequest("https://accounts.ea.com/connect/auth")
            {
                Method = Method.Get
            };

            request.AddParameter("access_token", Account.OriginPCToken);
            request.AddParameter("client_id", settingId);
            request.AddParameter("response_type", "code");
            request.AddParameter("release_type", "prod");

            request.AddHeader("User-Agent", "Mozilla/5.0 EA Download Manager Origin/10.5.94.46774");
            request.AddHeader("X-Origin-Platform", "PCWIN");
            request.AddHeader("localeInfo", "zh_TW");
            request.AddHeader("Cookie", $"remid={Account.Remid};sid={Account.Sid};");

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态码 {response.StatusCode}");

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求超时");
                return respResult;
            }

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                var localtion = response.Headers.ToList()
                    .Find(x => x.Name.Equals("location", StringComparison.OrdinalIgnoreCase))
                    .Value.ToString();

                LoggerHelper.Info($"{respResult.ApiName} 获取 localtion 为 {localtion}");
                if (localtion is not null)
                {
                    Account.LSXAuthCode = localtion.Split("=")[1];
                    LoggerHelper.Info($"{respResult.ApiName} 获取 AuthCode 成功 {Account.LSXAuthCode}");

                    respResult.IsSuccess = true;

                    UpdateCookie(response.Cookies, respResult.ApiName);
                }
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

    /// <summary>
    /// 通过AuthToken获取玩家头像信息
    /// </summary>
    public static async Task<RespResult> GetAvatarByUserId(string userId)
    {
        var respResult = new RespResult("GetAvatarByUserId Api");
        if (string.IsNullOrWhiteSpace(Account.AccessToken))
        {
            LoggerHelper.Warn($"AccessToken 为空，{respResult.ApiName} 请求终止");
            return respResult;
        }
        try
        {
            var request = new RestRequest($"https://api1.origin.com/avatar/user/{userId}/avatars")
            {
                Method = Method.Get
            };

            request.AddParameter("size", "1");

            request.AddHeader("Accept", "application/xml");
            request.AddHeader("AuthToken", Account.AccessToken);

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态 {response.ResponseStatus}");
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，状态码 {response.StatusCode}");

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;
            LoggerHelper.Info($"{respResult.ApiName} 请求结束，内容 {response.Content}");
            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"{respResult.ApiName} 请求超时");
                return respResult;
            }

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                respResult.IsSuccess = true;
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
