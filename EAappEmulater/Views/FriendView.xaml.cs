using EAappEmulater.Api;
using EAappEmulater.Utils;
using EAappEmulater.Models;
using EAappEmulater.Helper;

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
}
