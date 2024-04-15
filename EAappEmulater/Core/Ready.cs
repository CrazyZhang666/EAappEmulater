using EAappEmulater.Api;
using EAappEmulater.Utils;
using EAappEmulater.Helper;
using CommunityToolkit.Mvvm.Messaging;

namespace EAappEmulater.Core;

public static class Ready
{
    public static async void Run()
    {
        // 打开服务进程
        LoggerHelper.Info("正在启动服务进程...");
        ProcessHelper.OpenProcess(CoreUtil.File_Cache_EADesktop, true);
        ProcessHelper.OpenProcess(CoreUtil.File_Cache_OriginDebug, true);

        LoggerHelper.Info("正在启动 LSX 监听服务...");
        LSXTcpServer.Run();

        LoggerHelper.Info("正在启动 Battlelog 监听服务...");
        BattlelogHttpServer.Run();

        LoggerHelper.Info("正在加载玩家头像中...");
        await LoadAvatar();
    }

    public static void Stop()
    {
        // 保存配置文件
        Globals.Write();

        LoggerHelper.Info("正在关闭 LSX 监听服务...");
        LSXTcpServer.Stop();

        LoggerHelper.Info("正在关闭 Battlelog 监听服务...");
        BattlelogHttpServer.Stop();

        // 关闭服务进程
        CoreUtil.CloseServerProcess();
    }

    /// <summary>
    /// 非常重要，Api请求前置条件
    /// 刷新基础请求必备Token (d多个)
    /// </summary>
    public static async Task<bool> RefreshBaseTokens()
    {
        var result = await EaApi.GetToken();
        if (!result.IsSuccess)
        {
            LoggerHelper.Warn("刷新 Token 失败");
            return false;
        }
        LoggerHelper.Info("刷新 Token 成功");

        result = await EaApi.GetOriginPCAuth();
        if (!result.IsSuccess)
        {
            LoggerHelper.Warn("刷新 OriginPCAuth 失败");
            return false;
        }
        LoggerHelper.Info("刷新 OriginPCAuth 成功");

        result = await EaApi.GetOriginPCToken();
        if (!result.IsSuccess)
        {
            LoggerHelper.Warn("刷新 OriginPCToken 失败");
            return false;
        }
        LoggerHelper.Info("刷新 OriginPCToken 成功");

        return true;
    }

    /// <summary>
    /// 获取当前登录玩家信息
    /// </summary>
    public static async Task<bool> GetLoginAccountInfo()
    {
        LoggerHelper.Info("正在获取当前登录玩家信息...");
        var result = await EasyEaApi.GetLoginAccountInfo();
        if (result is null)
        {
            LoggerHelper.Warn("获取当前登录玩家信息失败");
            return false;
        }

        LoggerHelper.Info("获取当前登录玩家信息成功");
        var persona = result.personas.persona[0];

        Account.PlayerName = persona.displayName;
        LoggerHelper.Info($"玩家名称 {Account.PlayerName}");

        Account.PersonaId = persona.personaId.ToString();
        LoggerHelper.Info($"玩家PId {Account.PersonaId}");

        Account.UserId = persona.pidId.ToString();
        LoggerHelper.Info($"玩家UserId {Account.UserId}");

        if (string.IsNullOrWhiteSpace(Account.Avatar))
        {
            Account.Avatar = "Assets/Images/Avatars/Default.png";
            LoggerHelper.Warn("未发现玩家头像，将使用默认头像");
        }

        return true;
    }

    private static async Task LoadAvatar()
    {
        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                LoggerHelper.Error("加载玩家头像失败，请检查网络");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Warn($"加载玩家头像失败，开始第 {i} 次重试中...");
            }

            if (await GetUserAvatars())
            {
                LoggerHelper.Info($"加载玩家头像成功 {Account.Avatar}");
                WeakReferenceMessenger.Default.Send("", "LoadAvatar");
                return;
            }
        }
    }

    /// <summary>
    /// 获取当前登录玩家头像
    /// </summary>
    private static async Task<bool> GetUserAvatars()
    {
        LoggerHelper.Info("正在获取当前登录玩家头像...");
        var result = await EasyEaApi.GetUserAvatars(Account.UserId);
        if (result is null)
        {
            LoggerHelper.Warn("获取当前登录玩家头像失败");
            return false;
        }

        var avatar = result.users.First().avatar;
        Account.AvatarId = avatar.avatarId.ToString();
        LoggerHelper.Info("获取当前登录玩家头像成功");

        LoggerHelper.Info($"玩家AvatarId {Account.AvatarId}");

        // 开始缓存玩家头像到本地
        var savePath = Path.Combine(CoreUtil.Dir_Avatar, $"{Account.AvatarId}.png");
        if (!await CoreApi.DownloadWebImage(avatar.link, savePath))
        {
            LoggerHelper.Info($"下载玩家头像失败 {avatar.link}");
            return false;
        }

        LoggerHelper.Info($"下载玩家头像成功");
        Account.Avatar = savePath;
        LoggerHelper.Info($"玩家Avatar {Account.Avatar}");

        return true;
    }
}
