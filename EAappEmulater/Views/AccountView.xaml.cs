using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EAappEmulater.Core;
using EAappEmulater.Models;
using EAappEmulater.Windows;

namespace EAappEmulater.Views;

/// <summary>
/// AccountView.xaml 的交互逻辑
/// </summary>
public partial class AccountView : UserControl
{
    public AccountModel AccountModel { get; set; } = new();

    public AccountView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {
        AccountModel.AvatarId = Account.AvatarId;

        // 玩家头像为空处理（仅有数据账号）
        if (!string.IsNullOrWhiteSpace(Account.Remid) && string.IsNullOrWhiteSpace(Account.Avatar))
            AccountModel.Avatar = "Default";

        // 验证玩家头像与玩家头像Id是否一致
        if (!Account.Avatar.Contains(Account.AvatarId))
            AccountModel.Avatar = "Default";

        AccountModel.PlayerName = Account.PlayerName;
        AccountModel.PersonaId = Account.PersonaId;
        AccountModel.UserId = Account.UserId;

        AccountModel.Remid = Account.Remid;
        AccountModel.Sid = Account.Sid;
        AccountModel.Token = Account.AccessToken;

        //////////////////////////////////////////

        WeakReferenceMessenger.Default.Register<string, string>(this, "LoadAvatar", (s, e) =>
        {
            AccountModel.AvatarId = Account.AvatarId;
            AccountModel.Avatar = Account.Avatar;
        });
    }

    /// <summary>
    /// 更换账号
    /// </summary>
    [RelayCommand]
    private void SwitchAccount()
    {
        var accountWindow = new AccountWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = accountWindow;
        // 设置关闭标志
        MainWindow.IsCodeClose = true;
        // 关闭主窗口
        MainWindow.MainWinInstance.Close();

        // 显示更换账号窗口
        accountWindow.Show();
    }
}
