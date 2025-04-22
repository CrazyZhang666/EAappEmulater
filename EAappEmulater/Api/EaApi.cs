using EAappEmulater.Core;
using EAappEmulater.Helper;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using RestSharp;
using System.Management;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Web;
using System.Numerics;



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
        LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.UpdateCookieCount", apiName, cookies.Count));

        foreach (var item in cookies.ToList())
        {
            if (item.Name.Equals("remid", StringComparison.OrdinalIgnoreCase))
            {
                Account.Remid = item.Value;
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.UpdateCookieGetRemid", apiName, Account.Remid));
                IniHelper.WriteString("Cookie", "Remid", item.Value, Globals.GetAccountIniPath());
                continue;
            }

            if (item.Name.Equals("sid", StringComparison.OrdinalIgnoreCase))
            {
                Account.Sid = item.Value;
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.UpdateCookieGetSid", apiName, Account.Sid));
                IniHelper.WriteString("Cookie", "Sid", item.Value, Globals.GetAccountIniPath());
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

        try
        {
            var request = new RestRequest("https://accounts.ea.com/connect/auth")
            {
                Method = Method.Get
            };

            request.AddParameter("client_id", "JUNO_PC_CLIENT");
            request.AddParameter("response_type", "token");
            request.AddParameter("redirect_uri", "https://pc.ea.com/login.html");
            request.AddParameter("token_format", "JWT");
            request.AddParameter("pc_sign", HardwareInfo.GetPcSign());
            if (!String.IsNullOrEmpty(Account.Remid))
            {
                request.AddHeader("Cookie", $"remid={Account.Remid};sid={Account.Sid};");
            }

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatus", respResult.ApiName, response.ResponseStatus));
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatusCode", respResult.ApiName, response.StatusCode));

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;
            respResult.IsSuccess = false;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ErrorTimeout", respResult.ApiName));
                return null;
            }

            if (response.Content.Contains("error_code", StringComparison.OrdinalIgnoreCase))
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.GetTokenReqErrorExpiredCookie", respResult.ApiName, response.Headers.ToString()));
                return respResult;
            }

            if (response.StatusCode == HttpStatusCode.Redirect)
            {

                // 错误返回 {"error_code":"login_required","error":"login_required","error_number":"102100"}
                var location = response.Headers.ToList()
                    .Find(x => x.Name.Equals("location", StringComparison.OrdinalIgnoreCase))
                    .Value.ToString();
                if (string.IsNullOrEmpty(location))
                {
                    // 如果没有 "Location" 头部或包含 "#"，返回 null
                    return null;
                }
                if (location.StartsWith("https://signin.ea.com/p/juno/login?fid=")) {
                    respResult.Content = location;
                    return respResult;

                }

                string locationUrl = location.Replace("#", "?");
                var uri = new Uri(locationUrl);
                var query = HttpUtility.ParseQueryString(uri.Query);

                string accessToken = query["access_token"];
                string expiresStr = query["expires_in"];

                if (string.IsNullOrEmpty(accessToken)) {
                    LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.GetTokenReqErrorExpiredCookie", respResult.ApiName, query.ToString()));
                    return null;
                }

                Account.AccessToken = accessToken;
                Account.OriginPCToken = accessToken;
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.GetTokenReqSuccess", respResult.ApiName, Account.AccessToken));
                respResult.IsSuccess = true;

                UpdateCookie(response.Cookies, respResult.ApiName);
            }
            else
            {
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqError", respResult.ApiName, response.Content));
            }
        }
        catch (Exception ex)
        {
            respResult.Exception = ex.Message;
            LoggerHelper.Error(I18nHelper.I18n._("Api.EaApi.ReqErrorEx", respResult.ApiName, ex));
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
            LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ErrorNotFoundToken", respResult.ApiName));
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
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatus", respResult.ApiName, response.ResponseStatus));
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatusCode", respResult.ApiName, response.StatusCode));

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ErrorTimeout", respResult.ApiName));
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
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqError", respResult.ApiName, response.Content));
            }
        }
        catch (Exception ex)
        {
            respResult.Exception = ex.Message;
            LoggerHelper.Error(I18nHelper.I18n._("Api.EaApi.ReqErrorEx", respResult.ApiName, ex));
        }

        return respResult;
    }

    public static async Task<RespResult> GetAvatarByUserIds(List<string> userIds)
    {
        var respResult = new RespResult("GetAccountAvatarByUserId Api");

        if (string.IsNullOrWhiteSpace(Account.AccessToken))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ErrorNotFoundToken", respResult.ApiName));
            return respResult;
        }

        if (userIds == null || userIds.Count == 0)
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ErrorNotFoundUserId", respResult.ApiName));
            return respResult;
        }

        try
        {
            // 构建 GraphQL 批量查询
            var queryParts = userIds.Select((id, index) => $"u{index}: playerByPd(pd: {id}) {{ avatar {{ avatarId, large {{ path }} }} }}");
            var query = $"query {{ {string.Join(" ", queryParts)} }}";

            // 创建 GraphQL 客户端
            var graphQLClient = new GraphQLHttpClient("https://service-aggregation-layer.juno.ea.com/graphql", new NewtonsoftJsonSerializer());

            // 创建 GraphQL 请求
            var graphQLRequest = new GraphQLRequest
            {
                Query = query
            };

            graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Account.AccessToken}");
            var response = await graphQLClient.SendQueryAsync<object>(graphQLRequest);
            string responseContent = response.Data.ToString();

            respResult.StatusCode = HttpStatusCode.OK;
            respResult.Content = responseContent;

            LoggerHelper.Info($"{respResult.ApiName} 响应: {response.AsGraphQLHttpResponse().StatusCode}");

            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                respResult.IsSuccess = true;
            }
            else
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ReqErrorEmpty", respResult.ApiName));
            }
        }
        catch (Exception ex)
        {
            respResult.Exception = ex.Message;
            LoggerHelper.Error(I18nHelper.I18n._("Api.EaApi.ReqErrorEx", respResult.ApiName, ex));
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
            LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ErrorNotFoundToken", respResult.ApiName));
            return respResult;
        }

        if (string.IsNullOrWhiteSpace(Account.UserId))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ErrorNotFoundUserId", respResult.ApiName));
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
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatus", respResult.ApiName, response.ResponseStatus));
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatusCode", respResult.ApiName, response.StatusCode));

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ErrorTimeout", respResult.ApiName));
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
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqError", respResult.ApiName, response.Content));
            }
        }
        catch (Exception ex)
        {
            respResult.Exception = ex.Message;
            LoggerHelper.Error(I18nHelper.I18n._("Api.EaApi.ReqErrorEx", respResult.ApiName, ex));
        }

        return respResult;
    }


    /// <summary>
    /// 前置条件
    /// 1. GetToken
    /// 获取LSX游戏许可证
    /// </summary>
    public static async Task<RespResult> GetLSXLicense(string requestToken, string contentId)
    {
        var respResult = new RespResult("GetLSXLicense Api");

        if (string.IsNullOrWhiteSpace(Account.Remid) || string.IsNullOrWhiteSpace(Account.Sid))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ErrorNotFoundRemidOrSid", respResult.ApiName));
            return respResult;
        }

        if (string.IsNullOrWhiteSpace(Account.OriginPCToken))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ErrorNotFoundToken", respResult.ApiName));
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
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatus", respResult.ApiName, response.ResponseStatus));
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatusCode", respResult.ApiName, response.StatusCode));

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ErrorTimeout", respResult.ApiName));
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
                    LoggerHelper.Debug(I18nHelper.I18n._("Api.EaApi.GetLSXLicenseSuccess", respResult.ApiName, decryptArray[1]));

                    respResult.IsSuccess = true;

                    UpdateCookie(response.Cookies, respResult.ApiName);
                }
                else
                {
                    LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.GetLSXLicenseError", respResult.ApiName));
                }
            }
            else
            {
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqError", respResult.ApiName, response.Content));
            }
        }
        catch (Exception ex)
        {
            respResult.Exception = ex.Message;
            LoggerHelper.Error(I18nHelper.I18n._("Api.EaApi.ReqErrorEx", respResult.ApiName, ex));
        }

        return respResult;
    }

    /// <summary>
    /// 前置条件
    /// 1. GetToken
    /// 通过 cookie 获取 AutuCode (需要 settingId 作为 client_id 参数)
    /// 特殊版本，和网页登录账号获取 AutuCode 不同
    /// </summary>
    public static async Task<RespResult> GetLSXAutuCode(string settingId)
    {
        var respResult = new RespResult("GetLSXAutuCode Api");

        if (string.IsNullOrWhiteSpace(Account.Remid) || string.IsNullOrWhiteSpace(Account.Sid))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ErrorNotFoundRemidOrSid", respResult.ApiName));
            return respResult;
        }

        if (string.IsNullOrWhiteSpace(Account.OriginPCToken))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Api.EaApi.ErrorNotFoundToken", respResult.ApiName));
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
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatus", respResult.ApiName, response.ResponseStatus));
            LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqStatusCode", respResult.ApiName, response.StatusCode));

            respResult.StatusText = response.ResponseStatus;
            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ErrorTimeout", respResult.ApiName));
                return respResult;
            }

            respResult.StatusCode = response.StatusCode;
            respResult.Content = response.Content;

            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                var location = response.Headers.ToList()
                    .Find(x => x.Name.Equals("location", StringComparison.OrdinalIgnoreCase))
                    .Value.ToString();

                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.GetLSXAutuCodeLocation", respResult.ApiName, location));
                if (location is not null)
                {
                    Account.LSXAuthCode = location.Split("=")[1];
                    LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.GetLSXAutuCodeSuccess", respResult.ApiName, Account.LSXAuthCode));

                    respResult.IsSuccess = true;

                    UpdateCookie(response.Cookies, respResult.ApiName);
                }
            }
            else
            {
                LoggerHelper.Info(I18nHelper.I18n._("Api.EaApi.ReqError", respResult.ApiName, response.Content));
            }
        }
        catch (Exception ex)
        {
            respResult.Exception = ex.Message;
            LoggerHelper.Error(I18nHelper.I18n._("Api.EaApi.ReqErrorEx", respResult.ApiName, ex));
        }

        return respResult;
    }


    class HardwareInfo
    {
        public static string GetWMI(string className, string property)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {className}");
                foreach (ManagementObject obj in searcher.Get())
                    return obj[property]?.ToString()?.Trim();
            }
            catch { }
            return string.Empty;
        }

        public static string GetBIOSSerial() =>
            GetWMI("Win32_BIOS", "SerialNumber");

        public static string GetMotherboardSerial() =>
            GetWMI("Win32_BaseBoard", "SerialNumber");

        public static string GetHDDSerial() =>
            GetWMI("Win32_PhysicalMedia", "SerialNumber");

        public static int GetGPUDeviceIdFromPnP()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT DeviceID, Name FROM Win32_PnPEntity");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString() ?? "";
                    var deviceId = obj["DeviceID"]?.ToString() ?? "";

                    // 更精准判断：必须是 NVIDIA 且 DeviceID 来自 PCI 总线（不是 HDAUDIO）
                    if (deviceId.StartsWith("PCI\\VEN_10DE", StringComparison.OrdinalIgnoreCase))
                    {
                        var devMatch = Regex.Match(deviceId, @"DEV_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                        if (devMatch.Success)
                        {
                            return Convert.ToInt32(devMatch.Groups[1].Value, 16);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return 0;
        }

        public static string GetMacAddress()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up &&
                                       nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)?
                .GetPhysicalAddress().ToString();
        }

        public static string GenerateMID()
        {
            var raw = GetBIOSSerial()
                    + GetMotherboardSerial()
                    + GetHDDSerial()
                    + GetMacAddress();

            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

            BigInteger bigInt = new BigInteger(hashBytes.Append((byte)0).ToArray()); // 防止负数
            string digits = BigInteger.Abs(bigInt).ToString();

            return digits.PadLeft(19, '0').Substring(0, 19);
        }

        public static string GetTimestamp()
        {
            var now = DateTime.Now;
            return $"{now:yyyy-MM-dd H:m:s:fff}";
        }


        public static string GetPcSign()
        {
            var machineId = new
            {
                av = "v1",
                bsn = GetBIOSSerial(),
                gid = GetGPUDeviceIdFromPnP(),
                hsn = GetHDDSerial() ?? "To Be Filled By O.E.M.",
                mac = "$" + GetMacAddress(),
                mid = GenerateMID(),
                msn = GetMotherboardSerial(),
                sv = "v2",
                ts = GetTimestamp()
            };

            string json = JsonConvert.SerializeObject(machineId);
            string base64urlPayload = ToBase64Url(json);
            string secret = "nt5FfJbdPzNcl2pkC3zgjO43Knvscxft";
            string signature = CreateHmac(base64urlPayload, secret);
            return base64urlPayload + "." + signature;
        }

        public static string ToBase64Url(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            string base64 = Convert.ToBase64String(bytes);
            string base64Url = base64.Split('=')[0];
            base64Url = base64Url.Replace('+', '-').Replace('/', '_');

            return base64Url;
        }

        public static string CreateHmac(string data, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Base64UrlEncode(hashBytes);
            }
        }

        private static string Base64UrlEncode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            base64 = base64.Split('=')[0];
            base64 = base64.Replace('+', '-').Replace('/', '_');

            return base64;
        }
    }

}
