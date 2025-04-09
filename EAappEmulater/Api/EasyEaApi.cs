using EAappEmulater.Core;
using EAappEmulater.Helper;

namespace EAappEmulater.Api;

public static class EasyEaApi
{
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

    /// <summary>
    /// 下载玩家头像并生成lsx响应
    /// </summary>
    public static async Task<string> GetQueryImageXml(string id, string userid, string width, string imageid)
    {
        var savePath = string.Empty;
        string[] files = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache"), $"{userid}.*");
        string link = string.Empty;
        if (files.Length > 0)
        {
            LoggerHelper.Info(I18nHelper.I18n._("Api.EasyEaApi.FindLocalAvatarSkipDownload", files[0]));
            savePath = files[0];
        }
        else
        {
            var userIds = new List<string>
            {
                userid
            };

            var result = await EasyEaApi.GetAvatarByUserIds(userIds);
            if (result == null || !result.Values.Any(u => u?.avatar != null))
            {
                return string.Empty;
            }

            // 仅获取数组第一个
            var avatar = result.Values.First().avatar;             
            link = avatar.large.path.ToString();
            string fileName = link.Substring(link.LastIndexOf('/') + 1);
            savePath = savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache", fileName.Replace("416x416", userid));
            if (!await CoreApi.DownloadWebImage(link, savePath))
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Api.EasyEaApi.DownloadAvatarError", userid));
            }
        }
        var doc = new XmlDocument();
        var lsx = doc.CreateElement("LSX");
        doc.AppendChild(lsx);

        var response = doc.CreateElement("Response");
        response.SetAttribute("id", id);
        response.SetAttribute("sender", "EbisuSDK");
        lsx.AppendChild(response);

        var queryImageResponse = doc.CreateElement("QueryImageResponse");
        queryImageResponse.SetAttribute("Result", "0");
        response.AppendChild(queryImageResponse);

        var image = doc.CreateElement("Image");
        image.SetAttribute("Width", width);
        image.SetAttribute("ImageId", imageid);
        image.SetAttribute("Height", width);
        image.SetAttribute("ResourcePath", savePath);
        queryImageResponse.AppendChild(image);

        return doc.InnerXml;
    }
}
