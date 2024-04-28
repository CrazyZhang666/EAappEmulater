using EAappEmulater.Core;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using CommunityToolkit.Mvvm.Input;

namespace EAappEmulater.Windows;

/// <summary>
/// AccountWindow.xaml 的交互逻辑
/// </summary>
public partial class AccountWindow
{
    public ObservableCollection<AccountInfo> ObsCol_AccountInfos { get; set; } = new();

    public AccountWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private void Window_Account_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (var item in Account.AccountPathDb)
        {
            var account = new AccountInfo()
            {
                Index = (int)item.Key,
                IsUse = item.Key == Globals.AccountSlot,
                AccountSlot = item.Key,

                PlayerName = IniHelper.ReadString("Account", "PlayerName", item.Value),
                PersonaId = IniHelper.ReadString("Account", "PersonaId", item.Value),
                UserId = IniHelper.ReadString("Account", "UserId", item.Value),
                AvatarId = IniHelper.ReadString("Account", "AvatarId", item.Value),
                Avatar = IniHelper.ReadString("Account", "Avatar", item.Value),

                Remid = IniHelper.ReadString("Cookie", "Remid", item.Value),
                Sid = IniHelper.ReadString("Cookie", "Sid", item.Value),
                Token = IniHelper.ReadString("Cookie", "AccessToken", item.Value)
            };

            // 玩家头像为空处理（仅有数据账号）
            if (!string.IsNullOrWhiteSpace(account.Remid) && string.IsNullOrWhiteSpace(account.Avatar))
                account.Avatar = "Default";

            // 验证玩家头像与玩家头像Id是否一致
            if (!account.Avatar.Contains(account.AvatarId))
                account.Avatar = "Default";

            ObsCol_AccountInfos.Add(account);
        }

        ////////////////////////////////

        // 取配置文件
        Globals.Read();
        // 设置上次选中配置槽
        ListBox_AccountInfo.SelectedIndex = (int)Globals.AccountSlot;
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
    }

    /// <summary>
    /// 登录此账号
    /// </summary>
    [RelayCommand]
    private void LoginAccount()
    {
        if (ListBox_AccountInfo.SelectedItem is not AccountInfo item)
            return;

        // 设置当前选择配置槽
        Globals.AccountSlot = item.AccountSlot;
        // 保存新数据，防止丢失
        Globals.Write(true);

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
        if (ListBox_AccountInfo.SelectedItem is not AccountInfo item)
            return;

        // 设置当前选择配置槽
        Globals.AccountSlot = item.AccountSlot;
        // 保存新数据，防止丢失
        Globals.Write(true);

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
