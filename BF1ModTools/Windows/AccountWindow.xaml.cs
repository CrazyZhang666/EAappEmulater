using BF1ModTools.Core;
using BF1ModTools.Utils;
using BF1ModTools.Helper;
using BF1ModTools.Models;
using CommunityToolkit.Mvvm.Input;

namespace BF1ModTools.Windows;

/// <summary>
/// AccountWindow.xaml 的交互逻辑
/// </summary>
public partial class AccountWindow
{
    public AccountModel AccountModel { get; set; } = new();

    public AccountWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// 
    /// </summary>
    private void Window_Account_Loaded(object sender, RoutedEventArgs e)
    {
        Title = $"战地1模组工具箱 v{CoreUtil.VersionInfo} - {CoreUtil.GetIsAdminStr()}";

        Account.Read();

        // 仅展示用
        AccountModel.PlayerName = Account.PlayerName;
        // 可被修改
        AccountModel.Remid = Account.Remid;
        AccountModel.Sid = Account.Sid;
    }

    /// <summary>
    /// 窗口内容呈现完毕后事件
    /// </summary>
    private void Window_Account_ContentRendered(object sender, EventArgs e)
    {
    }

    /// <summary>
    /// 窗口关闭时事件
    /// </summary>
    private void Window_Account_Closing(object sender, CancelEventArgs e)
    {
        Account.Write();
    }

    /// <summary>
    /// 打开配置文件
    /// </summary>
    [RelayCommand]
    private void OpenConfigFolder()
    {
        ProcessHelper.OpenDirectory(CoreUtil.Dir_Default);
    }

    /// <summary>
    /// 保存数据
    /// </summary>
    private void SaveData()
    {
        // 仅展示用
        Account.PlayerName = AccountModel.PlayerName;
        // 可被修改
        Account.Remid = AccountModel.Remid;
        Account.Sid = AccountModel.Sid;

        Account.Write();
    }

    /// <summary>
    /// 登录选中账号
    /// </summary>
    [RelayCommand]
    private void LoginAccount()
    {
        // 保存数据
        SaveData();

        ////////////////////////////////

        var loadWindow = new LoadWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = loadWindow;
        // 关闭当前窗口
        this.Close();

        // 显示初始化窗口
        loadWindow.Show();
    }

    /// <summary>
    /// 获取Cookie
    /// </summary>
    [RelayCommand]
    private void GetCookie()
    {
        // 保存数据
        SaveData();

        ////////////////////////////////

        var loginWindow = new LoginWindow(false);

        // 转移主程序控制权
        Application.Current.MainWindow = loginWindow;
        // 关闭当前窗口
        this.Close();

        // 显示登录窗口
        loginWindow.Show();
    }

    /// <summary>
    /// 更换账号
    /// </summary>
    [RelayCommand]
    private void ChangeAccount()
    {
        // 清空当前账号信息
        Account.Reset();
        // 保存数据
        SaveData();

        ////////////////////////////////

        var loginWindow = new LoginWindow(true);

        // 转移主程序控制权
        Application.Current.MainWindow = loginWindow;
        // 关闭当前窗口
        this.Close();

        // 显示登录窗口
        loginWindow.Show();
    }
}
