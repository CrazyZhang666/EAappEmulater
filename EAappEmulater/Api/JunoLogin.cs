using EAappEmulater.Core;
using EAappEmulater.Helper;
using RestSharp;
using System.Text.RegularExpressions;
using System.Web;

namespace EAappEmulater.Api;

public sealed class JunoOAuthSession : IDisposable
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private int _completionState;

    internal JunoOAuthSession(string authorizationUrl, string codeVerifier)
    {
        AuthorizationUrl = authorizationUrl;
        CodeVerifier = codeVerifier;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string AuthorizationUrl { get; }
    internal string CodeVerifier { get; }
    internal DateTimeOffset CreatedAt { get; }

    internal bool TryBeginCompletion()
    {
        if (DateTimeOffset.UtcNow - CreatedAt > Lifetime)
        {
            return false;
        }

        return Interlocked.CompareExchange(ref _completionState, 1, 0) == 0;
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _completionState, 1);
    }
}

internal static class JunoLogin
{
    private const string AuthorizationEndpoint = "https://accounts.ea.com/connect/auth";
    private const string TokenEndpoint = "https://accounts.ea.com/connect/token";
    private const string ClientId = "JUNO_PC_CLIENT";
    private const string ClientSecret = "4mRLtYMb6vq9qglomWEaT4ChxsXWcyqbQpuBNfMPOYOiDmYYQmjuaBsF2Zp0RyVeWkfqhE9TuGgAw7te";
    private const string RedirectUri = "qrc:///html/login_successful.html";
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    internal static async Task<JunoOAuthSession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var codeVerifier = Base64UrlEncode(RandomBytes(32));
        var codeChallenge = CreateCodeChallenge(codeVerifier);
        var pcSign = await CreatePcSignSafeAsync(cancellationToken).ConfigureAwait(false);
        var query = HttpUtility.ParseQueryString(string.Empty);

        query["client_id"] = ClientId;
        query["sbiod_enabled"] = "false";
        query["response_type"] = "code";
        query["locale"] = "en_US";
        query["pc_sign"] = pcSign;
        query["nonce"] = BitConverter.ToInt32(RandomBytes(4), 0).ToString(CultureInfo.InvariantCulture);
        query["code_challenge_method"] = "S256";
        query["code_challenge"] = codeChallenge;

        return new JunoOAuthSession($"{AuthorizationEndpoint}?{query}", codeVerifier);
    }

    internal static async Task<RespResult> TrySilentLoginAsync(CancellationToken cancellationToken)
    {
        var result = CreateResult();
        var lockTaken = false;

        try
        {
            await LoginGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;

            var refreshToken = IniHelper.ReadString("Cookie", "RefreshToken", Globals.GetAccountIniPath());
            var refreshTokenRemidHash = IniHelper.ReadString("Cookie", "RefreshTokenRemidHash", Globals.GetAccountIniPath());
            if (!string.IsNullOrWhiteSpace(refreshToken) && IsRefreshTokenForCurrentAccount(refreshTokenRemidHash))
            {
                var refreshResult = await RefreshTokenAsync(refreshToken, cancellationToken).ConfigureAwait(false);
                if (refreshResult.IsSuccess)
                {
                    return refreshResult;
                }
            }
            else if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                IniHelper.WriteString("Cookie", "RefreshToken", string.Empty, Globals.GetAccountIniPath());
                IniHelper.WriteString("Cookie", "RefreshTokenRemidHash", string.Empty, Globals.GetAccountIniPath());
            }

            var cookieHeader = BuildCookieHeader(Account.Remid, Account.Sid);
            if (string.IsNullOrWhiteSpace(cookieHeader))
            {
                result.Exception = "EA login is required";
                return result;
            }

            using var session = await CreateSessionAsync(cancellationToken).ConfigureAwait(false);
            result.Content = session.AuthorizationUrl;

            using var client = CreateClient();
            var request = new RestRequest(session.AuthorizationUrl) { Method = Method.Get };
            request.AddHeader("Cookie", cookieHeader);

            var response = await client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            CopyResponseStatus(result, response);
            PersistCookies(response.Cookies);

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                result.Exception = "EA JUNO authorization timed out";
                return result;
            }

            var callbackUri = GetHeader(response, "Location");
            if (!IsRedirect(response.StatusCode) || !TryParseCallback(callbackUri, out _, out _))
            {
                result.Content = session.AuthorizationUrl;
                result.Exception = "EA login is required";
                return result;
            }

            return await CompleteLoginCoreAsync(session, callbackUri, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result.Exception = "EA login was canceled";
            return result;
        }
        catch (Exception ex)
        {
            result.Exception = ex.GetType().Name;
            LoggerHelper.Error($"EA JUNO login failed: {ex.GetType().Name}");
            return result;
        }
        finally
        {
            if (lockTaken)
            {
                LoginGate.Release();
            }
        }
    }

    internal static async Task<RespResult> CompleteLoginAsync(JunoOAuthSession session, string callbackUri, CancellationToken cancellationToken)
    {
        var result = CreateResult();
        var lockTaken = false;

        try
        {
            await LoginGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            return await CompleteLoginCoreAsync(session, callbackUri, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result.Exception = "EA login was canceled";
            return result;
        }
        catch (Exception ex)
        {
            result.Exception = ex.GetType().Name;
            LoggerHelper.Error($"EA JUNO token exchange failed: {ex.GetType().Name}");
            return result;
        }
        finally
        {
            if (lockTaken)
            {
                LoginGate.Release();
            }
        }
    }

    private static async Task<RespResult> CompleteLoginCoreAsync(JunoOAuthSession session, string callbackUri, CancellationToken cancellationToken)
    {
        var result = CreateResult();

        if (session == null)
        {
            result.Exception = "EA login session is missing";
            return result;
        }

        if (!TryParseCallback(callbackUri, out var authorizationCode, out var callbackError))
        {
            result.Exception = "Invalid EA qrc callback";
            return result;
        }

        if (!session.TryBeginCompletion())
        {
            result.Exception = "EA login session is expired or already completed";
            return result;
        }

        if (!string.IsNullOrWhiteSpace(callbackError))
        {
            result.Exception = "EA login was not completed";
            return result;
        }

        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            result.Exception = "EA qrc callback did not contain a code";
            return result;
        }

        using var client = CreateClient();
        var request = new RestRequest(TokenEndpoint) { Method = Method.Post };
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("code", authorizationCode);
        request.AddParameter("code_verifier", session.CodeVerifier);
        request.AddParameter("client_id", ClientId);
        request.AddParameter("client_secret", ClientSecret);
        request.AddParameter("redirect_uri", RedirectUri);
        request.AddParameter("token_format", "JWS");

        LoggerHelper.Info("Sending EA JUNO token exchange request.");
        var response = await client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        CopyResponseStatus(result, response);

        if (response.ResponseStatus == ResponseStatus.TimedOut)
        {
            return result;
        }

        if (!IsSuccess(response.StatusCode) || !TryReadTokenResponse(response.Content, out var accessToken, out var refreshToken, out var tokenType, out var expiresIn))
        {
            return result;
        }

        PersistCookies(response.Cookies);
        PersistTokens(accessToken, refreshToken, tokenType, expiresIn);
        result.Content = string.Empty;
        result.Exception = null;
        result.IsSuccess = true;
        return result;
    }

    private static async Task<RespResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var result = CreateResult();

        try
        {
            using var client = CreateClient();
            var request = new RestRequest(TokenEndpoint) { Method = Method.Post };
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", refreshToken);
            request.AddParameter("client_id", ClientId);
            request.AddParameter("client_secret", ClientSecret);

            LoggerHelper.Info("Sending EA JUNO refresh-token request.");
            var response = await client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            CopyResponseStatus(result, response);

            if (!IsSuccess(response.StatusCode) || !TryReadTokenResponse(response.Content, out var accessToken, out var rotatedRefreshToken, out var tokenType, out var expiresIn, false))
            {
                result.Exception = $"EA JUNO token refresh failed, HTTP {(int)response.StatusCode}";
                return result;
            }

            PersistCookies(response.Cookies);
            PersistTokens(accessToken, string.IsNullOrWhiteSpace(rotatedRefreshToken) ? refreshToken : rotatedRefreshToken, tokenType, expiresIn);
            result.Exception = null;
            result.IsSuccess = true;
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Exception = ex.GetType().Name;
            LoggerHelper.Warn($"EA JUNO token refresh failed: {ex.GetType().Name}");
            return result;
        }
    }

    private static RestClient CreateClient()
    {
        var options = new RestClientOptions { Timeout = TimeSpan.FromSeconds(20), FollowRedirects = false, ThrowOnAnyError = false, ThrowOnDeserializationError = false };
        return new RestClient(options);
    }

    private static RespResult CreateResult()
    {
        return new RespResult("GetToken Api") { IsSuccess = false, Content = string.Empty };
    }

    private static void CopyResponseStatus(RespResult result, RestResponse response)
    {
        result.StatusText = response.ResponseStatus;
        result.StatusCode = response.StatusCode;
        result.IsSuccess = false;
    }

    private static string BuildCookieHeader(string remid, string sid)
    {
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(remid) && IsSafeCookieValue(remid.Trim()))
        {
            values.Add($"remid={remid.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(sid) && IsSafeCookieValue(sid.Trim()))
        {
            values.Add($"sid={sid.Trim()}");
        }

        return string.Join("; ", values);
    }

    private static bool IsSafeCookieValue(string value)
    {
        foreach (var character in value)
        {
            var code = (int)character;
            var isCookieOctet = code == 0x21 || code >= 0x23 && code <= 0x2B || code >= 0x2D && code <= 0x3A || code >= 0x3C && code <= 0x5B || code >= 0x5D && code <= 0x7E;
            if (!isCookieOctet)
            {
                return false;
            }
        }

        return value.Length > 0;
    }

    private static bool TryParseCallback(string value, out string authorizationCode, out string error)
    {
        authorizationCode = null;
        error = null;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !uri.Scheme.Equals("qrc", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }

        if (!uri.AbsolutePath.Equals("/html/login_successful.html", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var query = HttpUtility.ParseQueryString(uri.Query);
        var fragment = HttpUtility.ParseQueryString(uri.Fragment.TrimStart('#'));
        authorizationCode = query["code"] ?? fragment["code"];
        error = query["error"] ?? query["error_code"] ?? fragment["error"] ?? fragment["error_code"];
        return true;
    }

    private static bool TryReadTokenResponse(string json, out string accessToken, out string refreshToken, out string tokenType, out long expiresIn, bool requireRefreshToken = false)
    {
        accessToken = null;
        refreshToken = null;
        tokenType = null;
        expiresIn = 0;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("error", out _) || root.TryGetProperty("error_code", out _) || root.TryGetProperty("error_number", out _))
            {
                return false;
            }

            accessToken = ReadJsonString(root, "access_token");
            refreshToken = ReadJsonString(root, "refresh_token");
            tokenType = ReadJsonString(root, "token_type");

            if (root.TryGetProperty("expires_in", out var expiresProperty))
            {
                if (!expiresProperty.TryGetInt64(out expiresIn))
                {
                    long.TryParse(expiresProperty.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out expiresIn);
                }
            }

            return !string.IsNullOrWhiteSpace(accessToken) && (!requireRefreshToken || !string.IsNullOrWhiteSpace(refreshToken));
        }
        catch
        {
            return false;
        }
    }

    private static string ReadJsonString(System.Text.Json.JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind == System.Text.Json.JsonValueKind.Null)
        {
            return null;
        }

        return property.ValueKind == System.Text.Json.JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static bool IsRefreshTokenForCurrentAccount(string expectedRemidHash)
    {
        if (string.IsNullOrWhiteSpace(Account.Remid) || string.IsNullOrWhiteSpace(expectedRemidHash))
        {
            return false;
        }

        var actualRemidHash = Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(Account.Remid.Trim())));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actualRemidHash), Encoding.ASCII.GetBytes(expectedRemidHash));
    }

    private static void PersistTokens(string accessToken, string refreshToken, string tokenType, long expiresIn)
    {
        var iniPath = Globals.GetAccountIniPath();
        Account.AccessToken = accessToken;
        Account.OriginPCToken = accessToken;
        IniHelper.WriteString("Cookie", "AccessToken", accessToken, iniPath);
        IniHelper.WriteString("Cookie", "OriginPCToken", accessToken, iniPath);

        if (!string.IsNullOrWhiteSpace(refreshToken) && !string.IsNullOrWhiteSpace(Account.Remid))
        {
            IniHelper.WriteString("Cookie", "RefreshToken", refreshToken, iniPath);
            var remidHash = Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(Account.Remid.Trim())));
            IniHelper.WriteString("Cookie", "RefreshTokenRemidHash", remidHash, iniPath);
        }
        else
        {
            IniHelper.WriteString("Cookie", "RefreshToken", string.Empty, iniPath);
            IniHelper.WriteString("Cookie", "RefreshTokenRemidHash", string.Empty, iniPath);
        }

        if (!string.IsNullOrWhiteSpace(tokenType))
        {
            IniHelper.WriteString("Cookie", "TokenType", tokenType, iniPath);
        }

        if (expiresIn > 0)
        {
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToUnixTimeSeconds();
            IniHelper.WriteString("Cookie", "AccessTokenExpiresAt", expiresAt.ToString(CultureInfo.InvariantCulture), iniPath);
        }
    }

    private static void PersistCookies(CookieCollection cookies)
    {
        if (cookies == null)
        {
            return;
        }

        foreach (Cookie cookie in cookies)
        {
            if (string.IsNullOrWhiteSpace(cookie.Value))
            {
                continue;
            }

            if (cookie.Name.Equals("remid", StringComparison.OrdinalIgnoreCase))
            {
                Account.Remid = cookie.Value;
                IniHelper.WriteString("Cookie", "Remid", cookie.Value, Globals.GetAccountIniPath());
            }
            else if (cookie.Name.Equals("sid", StringComparison.OrdinalIgnoreCase))
            {
                Account.Sid = cookie.Value;
                IniHelper.WriteString("Cookie", "Sid", cookie.Value, Globals.GetAccountIniPath());
            }
        }
    }

    private static string GetHeader(RestResponse response, string name)
    {
        if (response.Headers == null)
        {
            return null;
        }

        foreach (var header in response.Headers)
        {
            if (header.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value?.ToString();
            }
        }

        return null;
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var value = (int)statusCode;
        return value >= 300 && value < 400;
    }

    private static bool IsSuccess(HttpStatusCode statusCode)
    {
        var value = (int)statusCode;
        return value >= 200 && value < 300;
    }

    private static byte[] RandomBytes(int length)
    {
        var data = new byte[length];
        RandomNumberGenerator.Fill(data);
        return data;
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string CreateCodeChallenge(string codeVerifier)
    {
        return Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
    }

    private static async Task<string> CreatePcSignSafeAsync(CancellationToken cancellationToken)
    {
        var hardwareTask = Task.Run(() => CreatePcSign(true));
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        var completedTask = await Task.WhenAny(hardwareTask, timeoutTask).ConfigureAwait(false);

        if (completedTask == hardwareTask)
        {
            try
            {
                return await hardwareTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CreatePcSign(false);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        _ = hardwareTask.ContinueWith(task => _ = task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        return CreatePcSign(false);
    }

    private static string CreatePcSign(bool readHardware)
    {
        var boardManufacturer = readHardware ? ReadWmi("SELECT Manufacturer FROM Win32_BaseBoard", "Manufacturer", "Microsoft Corporation") : "Microsoft Corporation";
        var boardSerial = readHardware ? ReadWmi("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber", "None") : "None";
        var biosManufacturer = readHardware ? ReadWmi("SELECT Manufacturer FROM Win32_BIOS", "Manufacturer", "Microsoft Corporation") : "Microsoft Corporation";
        var biosSerial = readHardware ? ReadWmi("SELECT SerialNumber FROM Win32_BIOS", "SerialNumber", "None") : "None";
        var osInstallDate = readHardware ? ReadWmi("SELECT InstallDate FROM Win32_OperatingSystem", "InstallDate", "1970-01-0100:00:00.000000000+0000") : "1970-01-0100:00:00.000000000+0000";
        var osSerial = readHardware ? ReadWmi("SELECT SerialNumber FROM Win32_OperatingSystem", "SerialNumber", "None") : "None";
        var diskSerial = readHardware ? ReadWmi("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index = 0", "SerialNumber", "None") : "None";
        var gpuPnpId = readHardware ? ReadWmi("SELECT PNPDeviceID FROM Win32_VideoController", "PNPDeviceID", string.Empty) : string.Empty;

        var gpuId = 0u;
        var gpuMatch = Regex.Match(gpuPnpId, @"DEV_(\w+)", RegexOptions.IgnoreCase);
        if (gpuMatch.Success)
        {
            uint.TryParse(gpuMatch.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out gpuId);
        }

        var mac = readHardware ? GetEaMacAddress() : null;
        var midSource = boardManufacturer + boardSerial + biosManufacturer + biosSerial + osInstallDate + osSerial + (mac ?? string.Empty);
        var mid = Fnv1A64(Encoding.UTF8.GetBytes(midSource)).ToString(CultureInfo.InvariantCulture);
        var secretVersion = (RandomBytes(1)[0] & 1) == 0 ? "v1" : "v2";
        var payloadData = new Dictionary<string, object> { ["av"] = "v1", ["bsn"] = biosSerial, ["gid"] = gpuId, ["hsn"] = diskSerial };

        if (!string.IsNullOrWhiteSpace(mac))
        {
            payloadData["mac"] = mac;
        }

        payloadData["mid"] = mid;
        payloadData["msn"] = boardSerial;
        payloadData["sv"] = secretVersion;
        payloadData["ts"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss:fff", CultureInfo.InvariantCulture);

        var json = System.Text.Json.JsonSerializer.Serialize(payloadData);
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        var signKey = secretVersion == "v1" ? "ISa3dpGOc8wW7Adn4auACSQmaccrOyR2" : "nt5FfJbdPzNcl2pkC3zgjO43Knvscxft";
        using var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(signKey));
        var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(payload));
        return $"{payload}.{Base64UrlEncode(signature)}";
    }

    private static string ReadWmi(string query, string propertyName, string fallback)
    {
        try
        {
            var scope = new System.Management.ManagementScope(@"\\.\root\CIMV2");
            var objectQuery = new System.Management.ObjectQuery(query);
            var options = new System.Management.EnumerationOptions { ReturnImmediately = true, Rewindable = false, Timeout = TimeSpan.FromSeconds(2) };
            using var searcher = new System.Management.ManagementObjectSearcher(scope, objectQuery, options);

            foreach (System.Management.ManagementObject item in searcher.Get())
            {
                var value = item[propertyName]?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
        }
        catch
        {
        }

        return fallback;
    }

    private static string GetEaMacAddress()
    {
        try
        {
            foreach (var networkInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                var bytes = networkInterface.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length > 0)
                {
                    return "$" + BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static ulong Fnv1A64(byte[] data)
    {
        unchecked
        {
            const ulong offset = 0xcbf29ce484222325;
            const ulong prime = 0x100000001b3;
            var hash = offset;

            foreach (var value in data)
            {
                hash ^= value;
                hash *= prime;
            }

            return hash;
        }
    }
}
