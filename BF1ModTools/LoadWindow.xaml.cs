﻿using BF1ModTools.Api;
using BF1ModTools.Core;
using BF1ModTools.Utils;
using BF1ModTools.Helper;

namespace BF1ModTools;

/// <summary>
/// LoadWindow.xaml 的交互逻辑
/// </summary>
public partial class LoadWindow
{
    public LoadWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private void Window_Load_Loaded(object sender, RoutedEventArgs e)
    {
    }

    /// <summary>
    /// 窗口内容呈现完毕后事件
    /// </summary>
    private async void Window_Load_ContentRendered(object sender, EventArgs e)
    {
        // 开始验证Cookie有效性
        if (await CheckCookie())
        {
            // 如果Cookie有效，则开始初始化
            await InitGameInfo();
            return;
        }
        else
        {
            // 否则开始跳转登录窗口
            // 由于只是更新Cookie，所以不需要清理缓存
            var loginWindow = new LoginWindow
            {
                IsLogout = false
            };

            // 转移主程序控制权
            Application.Current.MainWindow = loginWindow;
            // 关闭初始化窗口
            this.Close();

            // 显示初始化窗口
            loginWindow.Show();
        }
    }

    /// <summary>
    /// 窗口关闭时事件
    /// </summary>
    private void Window_Load_Closing(object sender, CancelEventArgs e)
    {
    }

    /// <summary>
    /// 显示加载状态到UI界面
    /// </summary>
    private void DisplayLoadState(string log)
    {
        TextBlock_CheckState.Text = log;
    }

    /// <summary>
    /// 检查Cookie信息
    /// </summary>
    private async Task<bool> CheckCookie()
    {
        LoggerHelper.Info("开始初始化游戏信息...");

        // 先读取配置文件
        await Globals.Read();

        DisplayLoadState("正在检测玩家 Cookie 有效性...");
        LoggerHelper.Info("正在检测玩家 Cookie 有效性...");
        if (!await EasyEaApi.IsValidCookie())
        {
            LoggerHelper.Warn("玩家 Cookie 无效，准备跳转登录界面");
            return false;
        }

        LoggerHelper.Info("玩家 Cookie 有效");

        return true;
    }

    /// <summary>
    /// 初始化游戏信息
    /// </summary>
    private async Task InitGameInfo()
    {
        // 关闭服务进程
        await CoreUtil.CloseServiceProcess();

        DisplayLoadState("正在释放资源服务进程文件...");
        LoggerHelper.Info("正在释放资源服务进程文件...");
        FileHelper.ExtractResFile("Exec.EADesktop.exe", CoreUtil.File_Service_EADesktop);
        FileHelper.ExtractResFile("Exec.OriginDebug.exe", CoreUtil.File_Service_OriginDebug);

        /////////////////////////////////////////////////

        DisplayLoadState("正在刷新 BaseToken 数据...");
        LoggerHelper.Info("正在刷新 BaseToken 数据...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Hidden;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("刷新 BaseToken 数据失败，程序终止，请检查网络连接");
                LoggerHelper.Error("刷新 BaseToken 数据失败，程序终止，请检查网络连接");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState($"刷新 BaseToken 数据失败，开始第 {i} 次重试中...");
                LoggerHelper.Warn($"刷新 BaseToken 数据失败，开始第 {i} 次重试中...");
            }

            if (await Ready.RefreshBaseTokens())
            {
                LoggerHelper.Info("刷新 BaseToken 数据成功");
                break;
            }
        }

        DisplayLoadState("正在获取玩家账号信息...");
        LoggerHelper.Info("正在获取玩家账号信息...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Hidden;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("获取玩家账号信息失败，程序终止，请检查网络连接");
                LoggerHelper.Error("获取玩家账号信息失败，程序终止，请检查网络连接");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState($"获取玩家账号信息失败，开始第 {i} 次重试中...");
                LoggerHelper.Info($"获取玩家账号信息失败，开始第 {i} 次重试中...");
            }

            if (await Ready.GetLoginAccountInfo())
            {
                LoggerHelper.Info("获取玩家账号信息成功");
                break;
            }
        }

        /////////////////////////////////////////////////

        // 保存新数据，防止丢失
        Globals.Write();

        DisplayLoadState("初始化完成，开始启动主程序...");
        LoggerHelper.Info("初始化完成，开始启动主程序...");

        var mainWindow = new MainWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = mainWindow;
        // 关闭当前窗口
        this.Close();

        // 显示主窗口
        mainWindow.Show();
    }
}
