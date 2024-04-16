using BF1ModTools.Api;
using BF1ModTools.Helper;

namespace BF1ModTools.Core;

public static class Ready
{
    private static Timer _autoUpdateTimer;

    public static void Run()
    {
        LoggerHelper.Info("正在启动 LSX 监听服务...");
        LSXTcpServer.Run();

        // 定时刷新 BaseToken 数据
        LoggerHelper.Info("正在启动 定时刷新 BaseToken 服务...");
        _autoUpdateTimer = new Timer(AutoUpdateBaseToken, null, TimeSpan.FromHours(2), TimeSpan.FromHours(2));
        LoggerHelper.Info("启动 定时刷新 BaseToken 服务成功");
    }

    public static void Stop()
    {
        // 保存配置文件
        Globals.Write();

        LoggerHelper.Info("正在关闭 LSX 监听服务...");
        LSXTcpServer.Stop();

        LoggerHelper.Info("正在关闭 定时刷新 BaseToken 服务...");
        _autoUpdateTimer?.Dispose();
        _autoUpdateTimer = null;
        LoggerHelper.Info("关闭 定时刷新 BaseToken 服务成功");
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

            if (await RefreshBaseTokens())
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

        return true;
    }
}
