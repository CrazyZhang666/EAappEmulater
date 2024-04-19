using EAappEmulater.Core;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Linq;

namespace EAappEmulater.Views;

/// <summary>
/// AccountView.xaml 的交互逻辑
/// </summary>
public partial class AccountView : UserControl
{
    public ObservableCollection<AccountInfo> ObsCol_AccountInfos { get; set; } = new();

    public AccountView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
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

        ListBox_AccountInfo.SelectedIndex = (int)Globals.AccountSlot;

        //////////////////////////////////////////

        WeakReferenceMessenger.Default.Register<string, string>(this, "LoadAvatar", (s, e) =>
        {
            ObsCol_AccountInfos[(int)Globals.AccountSlot].AvatarId = Account.AvatarId;
            ObsCol_AccountInfos[(int)Globals.AccountSlot].Avatar = Account.Avatar;
        });
    }

    /// <summary>
    /// 切换账号
    /// </summary>
    [RelayCommand]
    private void SwitchAccount()
    {
        if (ListBox_AccountInfo.SelectedItem is not AccountInfo item)
            return;

        Globals.AccountSlot = item.AccountSlot;
        LoggerHelper.Info($"切换账号槽位成功 Globals AccountSlot {Globals.AccountSlot}");

        ////////////////////////////////

        var loadWindow = new LoadWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = loadWindow;
        // 关闭主窗窗口
        MainWindow.MainWindowInstance.Close();

        // 显示初始化窗口
        loadWindow.Show();
    }

    /// <summary>
    /// 注销登录
    /// </summary>
    [RelayCommand]
    private void LogoutAccount()
    {
        if (ListBox_AccountInfo.SelectedItem is not AccountInfo item)
            return;

        Globals.AccountSlot = item.AccountSlot;
        LoggerHelper.Info($"注销账号槽位成功 Globals AccountSlot {Globals.AccountSlot}");
        Account.Reset();

        ////////////////////////////////

        var loginWindow = new LoginWindow
        {
            IsLogout = true
        };

        // 转移主程序控制权
        Application.Current.MainWindow = loginWindow;
        // 关闭主窗窗口
        MainWindow.MainWindowInstance.Close();

        // 显示初始化窗口
        loginWindow.Show();
    }
}
