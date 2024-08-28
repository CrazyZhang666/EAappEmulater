using CommunityToolkit.Mvvm.Messaging;
using EAappEmulater.Api;
using EAappEmulater.Helper;
using EAappEmulater.Utils;

namespace EAappEmulater.Core;

public static class Ready
{
    private static Timer _autoUpdateTimer;

    public static async void Run()
    {
        // 打开服务进程
        LoggerHelper.Info("正在启动服务进程...");
        ProcessHelper.OpenProcess(CoreUtil.File_Service_EADesktop, true);
        ProcessHelper.OpenProcess(CoreUtil.File_Service_OriginDebug, true);

        LoggerHelper.Info("正在启动 LSX 监听服务...");
        LSXTcpServer.Run();

        LoggerHelper.Info("正在启动 Battlelog 监听服务...");
        BattlelogHttpServer.Run();

        // 加载玩家头像
        await LoadAvatar();

        // 检查EA App注册表
        RegistryHelper.CheckAndAddEaAppRegistryKey();

        // 定时刷新 BaseToken 数据
        LoggerHelper.Info("正在启动 定时刷新 BaseToken 服务...");
        _autoUpdateTimer = new Timer(AutoUpdateBaseToken, null, TimeSpan.FromHours(2), TimeSpan.FromHours(2));
        LoggerHelper.Info("启动 定时刷新 BaseToken 服务成功");
    }

    public static void Stop()
    {
        // 保存全局配置文件
        Globals.Write();

        // 保存账号配置文件
        Account.Write();

        LoggerHelper.Info("正在关闭 LSX 监听服务...");
        LSXTcpServer.Stop();

        LoggerHelper.Info("正在关闭 Battlelog 监听服务...");
        BattlelogHttpServer.Stop();

        LoggerHelper.Info("正在关闭 定时刷新 BaseToken 服务...");
        _autoUpdateTimer?.Dispose();
        _autoUpdateTimer = null;
        LoggerHelper.Info("关闭 定时刷新 BaseToken 服务成功");

        // 关闭服务进程
        CoreUtil.CloseServiceProcess();
    }

    /// <summary>
    /// 定时刷新 BaseToken 数据
    /// </summary>
    private static async void AutoUpdateBaseToken(object obj)
    {
        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                LoggerHelper.Error("定时刷新 BaseToken 数据失败，请检查网络连接");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Warn($"定时刷新 BaseToken 数据失败，开始第 {i} 次重试中...");
            }

            if (await RefreshBaseTokens(false))
            {
                LoggerHelper.Info("定时刷新 BaseToken 数据成功");
                break;
            }
        }
    }

    /// <summary>
    /// 非常重要，Api请求前置条件
    /// 刷新基础请求必备Token (多个)
    /// </summary>
    public static async Task<bool> RefreshBaseTokens(bool isInit = true)
    {
        // 根据情况刷新 Access Token
        if (!isInit)
        {
            // 如果是初始化，则这一步可以省略（因为重复了）
            // 但是定时刷新还是需要（因为有效期只有4小时）
            var result = await EaApi.GetToken();
            if (!result.IsSuccess)
            {
                LoggerHelper.Warn("刷新 Token 失败");
                return false;
            }
            LoggerHelper.Info("刷新 Token 成功");
        }

        //////////////////////////////////////

        // 刷新 OriginPCAuth
        {
            var result = await EaApi.GetOriginPCAuth();
            if (!result.IsSuccess)
            {
                LoggerHelper.Warn("刷新 OriginPCAuth 失败");
                return false;
            }
            LoggerHelper.Info("刷新 OriginPCAuth 成功");
        }

        // OriginPCToken
        {
            var result = await EaApi.GetOriginPCToken();
            if (!result.IsSuccess)
            {
                LoggerHelper.Warn("刷新 OriginPCToken 失败");
                return false;
            }
            LoggerHelper.Info("刷新 OriginPCToken 成功");
        }

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

        return true;
    }

    /// <summary>
    /// 加载登录玩家头像
    /// </summary>
    private static async Task LoadAvatar()
    {
        LoggerHelper.Info("正在获取当前登录玩家头像中...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                LoggerHelper.Error("获取当前登录玩家头像失败，请检查网络连接");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Info($"获取当前登录玩家头像，开始第 {i} 次重试中...");
            }

            // 只有头像Id为空才网络获取
            if (string.IsNullOrWhiteSpace(Account.AvatarId))
            {
                // 开始获取头像玩家Id
                if (await GetAvatarByUserIds())
                {
                    // 获取头像Id成功后下载头像
                    if (await DownloadAvatar())
                    {
                        return;
                    }
                }
            }
            else
            {
                // 获取头像Id成功后下载头像
                if (await DownloadAvatar())
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 批量获取玩家头像Id
    /// </summary>
    private static async Task<bool> GetAvatarByUserIds()
    {
        LoggerHelper.Info("正在获取当前登录玩家头像Id中...");

        var userIds = new List<string>
        {
            Account.UserId
        };

        var result = await EasyEaApi.GetAvatarByUserIds(userIds);
        if (result is null)
        {
            LoggerHelper.Warn("获取当前登录玩家头像Id失败");
            return false;
        }

        // 仅获取数组第一个
        var avatar = result.users.First().avatar;
        Account.AvatarId = avatar.avatarId.ToString();

        LoggerHelper.Info("获取当前登录玩家头像Id成功");
        LoggerHelper.Info($"玩家 AvatarId {Account.AvatarId}");

        return true;
    }

    /// <summary>
    /// 下载玩家头像
    /// </summary>
    private static async Task<bool> DownloadAvatar(bool isOverride = true)
    {
        string[] files = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache"), $"{Account.UserId}.*");
        var savePath = string.Empty;
        string link = string.Empty;
        if (files.Length > 0)
        {
            Account.Avatar = files[0];
            LoggerHelper.Info($"发现本地玩家头像图片缓存，跳过网络下载操作 {Account.Avatar}");
            WeakReferenceMessenger.Default.Send("", "LoadAvatar");
            return true;
        }

        var result = await EaApi.GetAvatarByUserId(Account.UserId);
        if (!result.IsSuccess)
        {
            LoggerHelper.Warn($"下载当前登录玩家头像失败 {Account.UserId}");
            return false;
        }
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(result.Content);
        XmlNode linkNode = xmlDoc.SelectSingleNode("//link");
        link = linkNode.InnerText;
        string fileName = link.Substring(link.LastIndexOf('/') + 1);
        savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache", fileName.Replace("208x208", Account.UserId));
        if (!await CoreApi.DownloadWebImage(link, savePath))
        {
            LoggerHelper.Warn($"下载当前登录玩家头像失败 {Account.UserId}");
            return false;
        }
        Account.Avatar = savePath;

        LoggerHelper.Info($"下载当前登录玩家头像成功");
        LoggerHelper.Info($"玩家 Avatar {Account.Avatar}");

        WeakReferenceMessenger.Default.Send("", "LoadAvatar");

        return true;
    }
}
