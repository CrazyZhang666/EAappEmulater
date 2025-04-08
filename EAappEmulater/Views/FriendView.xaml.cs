using CommunityToolkit.Mvvm.Messaging;
using EAappEmulater.Api;
using EAappEmulater.Core;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using EAappEmulater.Utils;

namespace EAappEmulater.Views;

/// <summary>
/// FriendView.xaml 的交互逻辑
/// </summary>
public partial class FriendView : UserControl
{
    public ObservableCollection<FriendInfo> ObsCol_FriendInfos { get; set; } = new();

    private List<FriendInfo> _friendInfoList = new();

    public FriendView()
    {
        InitializeComponent();

        ToDoList();
    }

    private async void ToDoList()
    {
        Globals.IsGetFriendsSuccess = false;
        Globals.FriendsXmlString = string.Empty;
        Globals.QueryPresenceString = string.Empty;

        LoggerHelper.Info("正在获取当前账号好友列表中...");

        try
        {
            // 最多执行4次
            for (int i = 0; i <= 4; i++)
            {
                // 当第4次还是失败，终止程序
                if (i > 3)
                {
                    LoggerHelper.Error("获取当前账号好友列表失败，请检查网络连接");
                    return;
                }

                // 第1次不提示重试
                if (i > 0)
                {
                    LoggerHelper.Info($"获取当前账号好友列表，开始第 {i} 次重试中...");
                }

                var friends = await EasyEaApi.GetUserFriends();
                if (friends is not null)
                {
                    LoggerHelper.Info("获取当前账号好友列表成功");
                    LoggerHelper.Info($"当前账号好友数量为 {friends.entries.Count}");
                    

                    foreach (var entry in friends.entries)
                    {
                        _friendInfoList.Add(new()
                        {
                            Avatar = "Default",
                            DiffDays = CoreUtil.GetDiffDays(entry.timestamp),
                            DisplayName = entry.displayName ?? string.Empty,
                            NickName = entry.nickName,
                            UserId = entry.userId,
                            PersonaId = entry.personaId,
                            FriendType = entry.friendType,
                            DateTime = CoreUtil.TimestampToDataTimeString(entry.timestamp)
                        });
                    }

                    // 升序排序
                    // 如果 DisplayName 为 null 或者 其他国家字符，则可能会抛出异常
                    _friendInfoList = _friendInfoList.OrderBy(p => p.DisplayName, StringComparer.InvariantCulture).ToList();

                    var index = 0;
                    foreach (var friendInfo in _friendInfoList)
                    {
                        friendInfo.Index = ++index;
                        ObsCol_FriendInfos.Add(friendInfo);
                    }

                    // 选中第一个
                    if (ObsCol_FriendInfos.Count != 0)
                        ListBox_FriendInfo.SelectedIndex = 0;

                    // 生成好友列表字符串
                    GenerateXmlString();
                    GenerateXmlStringForQueryPresence();
                    // 获取好友头像
                    LoggerHelper.Info("准备获取好友头像");
                    UpdateFriendsAvatars();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("获取当前账号好友列表发生异常", ex);
        }
    }

    private void GenerateXmlString()
    {
        if (_friendInfoList.Count == 0)
            return;

        var doc = new XmlDocument();

        var lsx = doc.CreateElement("LSX");
        doc.AppendChild(lsx);

        var response = doc.CreateElement("Response");
        response.SetAttribute("id", "##ID##");
        response.SetAttribute("sender", "XMPP");
        lsx.AppendChild(response);

        var queryFreiResp = doc.CreateElement("QueryFriendsResponse");
        response.AppendChild(queryFreiResp);

        foreach (var friendInfo in _friendInfoList)
        {
            var friend = doc.CreateElement("Friend");

            var title = MiscUtil.GetRandomFriendTitle();

            friend.SetAttribute("RichPresence", title);
            friend.SetAttribute("AvatarId", "##AvatarId##");
            friend.SetAttribute("UserId", $"{friendInfo.UserId}");
            friend.SetAttribute("Group", "");
            friend.SetAttribute("Title", title);
            friend.SetAttribute("TitleId", "Origin.OFR.50.0004152");
            friend.SetAttribute("GamePresence", "");
            friend.SetAttribute("Persona", friendInfo.DisplayName);
            friend.SetAttribute("PersonaId", $"{friendInfo.PersonaId}");
            friend.SetAttribute("State", "MUTUAL");
            friend.SetAttribute("MultiplayerId", "196216");
            friend.SetAttribute("GroupId", "");
            friend.SetAttribute("Presence", "INGAME");

            queryFreiResp.AppendChild(friend);
        }

        Globals.FriendsXmlString = doc.InnerXml;
        Globals.IsGetFriendsSuccess = true;
    }
    private void GenerateXmlStringForQueryPresence()
    {
        if (_friendInfoList.Count == 0)
            return;

        var doc = new XmlDocument();

        var lsx = doc.CreateElement("LSX");
        doc.AppendChild(lsx);

        var response = doc.CreateElement("Response");
        response.SetAttribute("id", "##ID##");
        response.SetAttribute("sender", "XMPP");
        lsx.AppendChild(response);

        var queryFreiResp = doc.CreateElement("QueryPresenceResponse");
        response.AppendChild(queryFreiResp);

        foreach (var friendInfo in _friendInfoList)
        {
            var friend = doc.CreateElement("Friend");

            var title = MiscUtil.GetRandomFriendTitle();

            friend.SetAttribute("RichPresence", title);
            friend.SetAttribute("AvatarId", "##AvatarId##");
            friend.SetAttribute("UserId", $"{friendInfo.UserId}");
            friend.SetAttribute("Group", "");
            friend.SetAttribute("Title", title);
            friend.SetAttribute("TitleId", "Origin.OFR.50.0004152");
            friend.SetAttribute("GamePresence", "");
            friend.SetAttribute("Persona", friendInfo.DisplayName);
            friend.SetAttribute("PersonaId", $"{friendInfo.PersonaId}");
            friend.SetAttribute("State", "MUTUAL");
            friend.SetAttribute("MultiplayerId", "196216");
            friend.SetAttribute("GroupId", "");
            friend.SetAttribute("Presence", "INGAME");

            queryFreiResp.AppendChild(friend);
        }

        // 手动添加一个额外的 Friend 元素，这个就是玩家本人的信息
        var extraFriend = doc.CreateElement("Friend");
        extraFriend.SetAttribute("RichPresence", "");
        extraFriend.SetAttribute("AvatarId", "");
        extraFriend.SetAttribute("UserId", "##UID##");
        extraFriend.SetAttribute("Group", "");
        extraFriend.SetAttribute("Title", "");
        extraFriend.SetAttribute("TitleId", "");
        extraFriend.SetAttribute("GamePresence", "");
        extraFriend.SetAttribute("Persona", "");
        extraFriend.SetAttribute("PersonaId", "------");
        extraFriend.SetAttribute("State", "NONE");
        extraFriend.SetAttribute("MultiplayerId", "");
        extraFriend.SetAttribute("GroupId", "");
        extraFriend.SetAttribute("Presence", "UNKNOWN");

        queryFreiResp.AppendChild(extraFriend);

        Globals.QueryPresenceString = doc.InnerXml;
    }

    private async void UpdateFriendsAvatars()
    {
        var userIdList = ObsCol_FriendInfos
                    .Select(e => e.UserId.ToString())
                    .Distinct()                      
                    .ToList();

        // 获取所有用户的头像信息
        var avatarResult = await EasyEaApi.GetAvatarByUserIds(userIdList);
        if (avatarResult == null)
        {
            LoggerHelper.Warn("获取好友头像失败");
            return;
        }

        // 构建一个字典，将 userId 映射到对应的头像信息
        var avatarMap = new Dictionary<long, AvatarInfo>();
        // 使用下标索引来根据 userIdList 映射头像
        foreach (var kvp in avatarResult)
        {
            // 确保 kvp.Key 是形如 "u0", "u1", "u2" 等
            if (kvp.Key.StartsWith("u"))
            {
                // 提取下标 X
                if (int.TryParse(kvp.Key.Substring(1), out int index) && index < userIdList.Count)
                {
                    var userId = userIdList[index]; // 根据索引取对应的 userId
                    if (long.TryParse(userId, out long uid))
                    {
                        if (kvp.Value?.avatar != null)
                        {
                            var avatarInfo = new AvatarInfo
                            {
                                AvatarId = kvp.Value.avatar.avatarId,
                                Url = kvp.Value.avatar.large?.path
                            };
                            avatarMap[uid] = avatarInfo; // 使用 long 类型的 uid 作为键
                        }
                    }
                    else
                    {
                        LoggerHelper.Warn($"无法解析 userId: {userId}");
                    }
                }
                else
                {
                    LoggerHelper.Warn($"无法解析 kvp.Key: {kvp.Key.Substring(1)}");
                }
            }
        }

        // 遍历每个好友信息，更新头像
        foreach (var friendInfo in ObsCol_FriendInfos)
        {
            if (avatarMap.ContainsKey(friendInfo.UserId))
            {
                var avatarInfo = avatarMap[friendInfo.UserId];
                // 下载头像
                string downloadedAvatarUrl = await DownloadAvatarByUserId(friendInfo.UserId.ToString(), avatarInfo.AvatarId.ToString(), avatarInfo.Url);

                // 如果下载成功，更新 Avatar 字段，否则设置为 "Default"
                friendInfo.Avatar = downloadedAvatarUrl ?? "Default";
            }
            else
            {
                // 没有找到头像信息时，设置为默认值
                friendInfo.Avatar = "Default";
                LoggerHelper.Warn($"没有找到好友 {friendInfo.UserId} 的头像");
            }
        }

        // 发送消息通知加载头像
        WeakReferenceMessenger.Default.Send("", "LoadAvatar");
    }

    /// <summary>
    /// 下载玩家头像
    /// </summary>
    private static async Task<string> DownloadAvatarByUserId(string UserId, string AvatarId, string AvatarUrl)
    {
        string[] files = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache"), $"{AvatarId}.*");
        var savePath = string.Empty;
        if (files.Length > 0)
        {
            return files[0];
        }

        string fileName = AvatarUrl.Substring(AvatarUrl.LastIndexOf('/') + 1);
        savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache", fileName.Replace("416x416", UserId));
        if (!await CoreApi.DownloadWebImage(AvatarUrl, savePath))
        {
            LoggerHelper.Warn($"下载玩家头像失败 {AvatarId}");
            return null;
        }
        return savePath;
    }
}
