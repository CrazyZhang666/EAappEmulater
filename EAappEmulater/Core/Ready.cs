using CommunityToolkit.Mvvm.Messaging;
using EAappEmulater.Api;
using EAappEmulater.Helper;
using EAappEmulater.Utils;
using Microsoft.VisualBasic.ApplicationServices;

namespace EAappEmulater.Core;

public static class Ready
{
    private static Timer _autoUpdateTimer;

    public static async void Run()
    {
        // 打开服务进程
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.StartProcess"));

        ProcessHelper.OpenProcess(CoreUtil.File_Service_OriginDebug, true);

        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.StartLSXListen"));
        LSXTcpServer.Run();

        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.StartBattlelogListen"));
        BattlelogHttpServer.Run();

        // 加载玩家头像
        await LoadAvatar();

        // 检查EA App注册表
        RegistryHelper.CheckAndAddEaAppRegistryKey();

        // 定时刷新 BaseToken 数据
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.StartUpdateToken"));
        _autoUpdateTimer = new Timer(AutoUpdateBaseToken, null, TimeSpan.FromHours(2), TimeSpan.FromHours(2));
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.StartUpdateTokenSuccess"));
    }

    public static void Stop()
    {
        // 保存全局配置文件
        Globals.Write();

        // 保存账号配置文件
        Account.Write();

        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.StopLSXListen"));
        LSXTcpServer.Stop();

        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.StopBattlelogListen"));
        BattlelogHttpServer.Stop();

        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.StopUpdateToken"));
        _autoUpdateTimer?.Dispose();
        _autoUpdateTimer = null;
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.StopUpdateTokenSuccess"));

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
                LoggerHelper.Error(I18nHelper.I18n._("Core.Ready.AutoUpdateTokenErrorNetwork"));
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Warn(I18nHelper.I18n._("Core.Ready.AutoUpdateTokenErrorRetry", i));
            }

            if (await RefreshBaseTokens(false))
            {
                LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.AutoUpdateTokenSuccess"));
                break;
            }
        }
    }

    /// <summary>
    /// 非常重要，Api请求前置条件
    /// 由于Origin停运, 更改为EA App Token获取
    /// 只需要获取一个Token即可
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
                LoggerHelper.Warn(I18nHelper.I18n._("Core.Ready.RefreshTokenError"));
                return false;
            }
            LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.RefreshTokenSuccess"));
        }

        return true;
    }

    /// <summary>
    /// 获取当前登录玩家信息
    /// </summary>
    public static async Task<bool> GetLoginAccountInfo()
    {
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.GetLoginAccountInfoProcess"));
        var result = await EasyEaApi.GetLoginAccountInfo();
        if (result is null)
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Core.Ready.GetLoginAccountInfoError"));
            return false;
        }

        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.GetLoginAccountInfoSuccess"));
        LoggerHelper.Info($"{result.personas.ToString()}");
        var persona = result.personas.persona
            .FirstOrDefault(p => p.namespaceName == "cem_ea_id");

        if (persona == null)
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Core.Ready.GetLoginAccountInfoErrorNotFound"));
            return false;
        }

        Account.PlayerName = persona.displayName;
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.GetLoginAccountInfoPlayerName", Account.PlayerName));

        Account.PersonaId = persona.personaId.ToString();
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.GetLoginAccountInfoPersonaId", Account.PersonaId));

        Account.UserId = persona.pidId.ToString();
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.GetLoginAccountInfoUserId", Account.UserId));

        return true;
    }

    /// <summary>
    /// 加载登录玩家头像
    /// </summary>
    private static async Task LoadAvatar()
    {
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.LoadAvatarProcess"));

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                LoggerHelper.Error(I18nHelper.I18n._("Core.Ready.LoadAvatarErrorNetwork"));
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.LoadAvatarErrorRetry", i));
            }

            // 只有头像Id为空才网络获取
            if (string.IsNullOrWhiteSpace(Account.AvatarId))
            {
                // 开始获取头像玩家Id
                if (await GetAccountAvatarByUserId())
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
    /// 获取当前登录玩家头像Id
    /// </summary>
    private static async Task<bool> GetAccountAvatarByUserId()
    {
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.GetAccountAvatarByUserIdProcess"));

        var userIds = new List<string>
        {
            Account.UserId
        };

        var result = await EasyEaApi.GetAvatarByUserIds(userIds);
        if (result == null || !result.Values.Any(u => u?.avatar != null))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Core.Ready.GetAccountAvatarByUserIdError"));
            return false;
        }

        // 仅获取数组第一个
        var avatar = result.Values.First().avatar;
        Account.AvatarId = avatar.avatarId.ToString();

        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.GetAccountAvatarByUserIdSuccess"));
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.GetAccountAvatarByUserId", Account.AvatarId));

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
            LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.DownloadAvatarSkip", Account.Avatar));
            WeakReferenceMessenger.Default.Send("", "LoadAvatar");
            return true;
        }
        var userIds = new List<string>
            {
                Account.UserId
            };

        var result = await EasyEaApi.GetAvatarByUserIds(userIds);
        if (result == null || !result.Values.Any(u => u?.avatar != null))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Core.Ready.DownloadAvatarError", Account.UserId));
            return false;
        }

        // 仅获取数组第一个
        var avatar = result.Values.First().avatar;
        link = avatar.large.path.ToString();
        string fileName = link.Substring(link.LastIndexOf('/') + 1);
        savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache", fileName.Replace("416x416", Account.UserId));
        if (!await CoreApi.DownloadWebImage(link, savePath))
        {
            LoggerHelper.Warn(I18nHelper.I18n._("Core.Ready.DownloadAvatarError", Account.UserId));
            return false;
        }
        Account.Avatar = savePath;

        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.DownloadAvatarSuccess", Account.UserId));
        LoggerHelper.Info(I18nHelper.I18n._("Core.Ready.DownloadAvatar", Account.Avatar));

        WeakReferenceMessenger.Default.Send("", "LoadAvatar");

        return true;
    }
}
