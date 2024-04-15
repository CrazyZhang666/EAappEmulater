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

    public FriendView()
    {
        InitializeComponent();

        ToDoList();
    }

    private async void ToDoList()
    {
        LoggerHelper.Info("正在获取当前登录账号玩家列表中...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                LoggerHelper.Error("获取当前登录玩家列表失败，请检查网络连接");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Info($"获取当前登录玩家列表，开始第 {i} 次重试中...");
            }

            var friends = await EasyEaApi.GetUserFriends();
            if (friends is not null)
            {
                LoggerHelper.Info("获取当前登录玩家列表成功");

                var index = 0;
                foreach (var entry in friends.entries)
                {
                    ObsCol_FriendInfos.Add(new()
                    {
                        Index = ++index,
                        Avatar = "Assets/Images/Avatars/Default.png",
                        DisplayName = entry.displayName,
                        NickName = entry.nickName,
                        UserId = entry.userId,
                        PersonaId = entry.personaId,
                        FriendType = entry.friendType,
                        DateTime = CoreUtil.TimestampToDataTimeString(entry.timestamp)
                    });
                }

                break;
            }
        }
    }
}
