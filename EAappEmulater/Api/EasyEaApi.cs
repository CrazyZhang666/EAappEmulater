using EAappEmulater.Core;
using EAappEmulater.Helper;

namespace EAappEmulater.Api;

public static class EasyEaApi
{
    /// <summary>
    /// 验证 cookie 是否有效
    /// </summary>
    public static async Task<bool> IsValidCookie()
    {
        var result = await EaApi.GetToken();
        return result.IsSuccess;
    }

    /// <summary>
    /// 获取 LSX 监听服务所需的 AutuCode
    /// </summary>
    public static async Task<string> GetLSXAutuCode(string settingId)
    {
        var result = await EaApi.GetLSXAutuCode(settingId);
        if (!result.IsSuccess)
            return string.Empty;

        return Account.LSXAuthCode;
    }

    /// <summary>
    /// 获取 LSX 监听服务所需的许可证 License
    /// </summary>
    public static async Task<string> GetLSXLicense(string requestToken, string contentId)
    {
        var result = await EaApi.GetLSXLicense(requestToken, contentId);
        if (!result.IsSuccess)
            return string.Empty;

        return result.Content;
    }

    /// <summary>
    /// 获取登录玩家账号信息
    /// </summary>
    public static async Task<Identity> GetLoginAccountInfo()
    {
        var result = await EaApi.GetIdentityMe();
        if (!result.IsSuccess)
            return null;

        return JsonHelper.JsonDeserialize<Identity>(result.Content);
    }

    /// <summary>
    /// 批量获取玩家头像
    /// </summary>
    public static async Task<Avatars> GetAvatarByUserIds(List<string> userIds)
    {
        var result = await EaApi.GetAvatarByUserIds(userIds);
        if (!result.IsSuccess)
            return null;

        return JsonHelper.JsonDeserialize<Avatars>(result.Content);
    }

    /// <summary>
    /// 获取登录玩家好友列表
    /// </summary>
    public static async Task<Friends> GetUserFriends()
    {
        var result = await EaApi.GetUserFriends();
        if (!result.IsSuccess)
            return null;

        return JsonHelper.JsonDeserialize<Friends>(result.Content);
    }
}
