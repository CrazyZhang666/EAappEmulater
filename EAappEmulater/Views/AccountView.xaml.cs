using EAappEmulater.Core;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using CommunityToolkit.Mvvm.Input;

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
            ObsCol_AccountInfos.Add(new()
            {
                Index = (int)item.Key,
                IsUse = item.Key == Globals.AccountSlot,
                AccountSlot = item.Key,

                PlayerName = IniHelper.ReadString("Account", "PlayerName", item.Value),
                PersonaId = IniHelper.ReadString("Account", "PersonaId", item.Value),
                UserId = IniHelper.ReadString("Account", "UserId", item.Value),
                Avatar = IniHelper.ReadString("Account", "Avatar", item.Value),

                Remid = IniHelper.ReadString("Cookie", "Remid", item.Value),
                Sid = IniHelper.ReadString("Cookie", "Sid", item.Value)
            });
        }

        ListBox_AccountInfo.SelectedIndex = (int)Globals.AccountSlot;
    }

    private void RunLoadWindow(bool isLogout = false)
    {
        var loadWindow = new LoadWindow
        {
            IsLogout = isLogout
        };

        // 转移主程序控制权
        Application.Current.MainWindow = loadWindow;
        // 关闭主窗窗口
        MainWindow.MainWindowInstance.Close();

        // 显示初始化窗口
        loadWindow.Show();
    }

    [RelayCommand]
    private async Task LoadAvatar()
    {
        var index = ListBox_AccountInfo.SelectedIndex;

        if (string.IsNullOrWhiteSpace(ObsCol_AccountInfos[index].UserId))
        {
            LoggerHelper.Warn("玩家 UserId 为空，操作取消");
            NotifierHelper.Warning("玩家 UserId 为空，操作取消");
        }

        LoggerHelper.Info("正在获取玩家头像中...");
        NotifierHelper.Notice("正在获取玩家头像中...");

        if (await Ready.GetUserAvatars())
        {
            ObsCol_AccountInfos[index].Avatar = Account.Avatar;

            LoggerHelper.Info($"获取玩家头像成功 {Account.Avatar}");
            NotifierHelper.Success("获取玩家头像成功");

            return;
        }

        LoggerHelper.Warn("获取玩家头像失败");
        NotifierHelper.Warning("获取玩家头像失败");
    }

    [RelayCommand]
    private void SwitchAccount()
    {
        if (ListBox_AccountInfo.SelectedItem is not AccountInfo item)
            return;

        Globals.AccountSlot = item.AccountSlot;
        LoggerHelper.Info($"切换账号槽位成功 Globals AccountSlot {Globals.AccountSlot}");

        RunLoadWindow();
    }

    [RelayCommand]
    private void LogoutAccount()
    {
        if (ListBox_AccountInfo.SelectedItem is not AccountInfo item)
            return;

        Globals.AccountSlot = item.AccountSlot;
        LoggerHelper.Info($"注销账号槽位成功 Globals AccountSlot {Globals.AccountSlot}");
        Account.Reset();

        RunLoadWindow(true);
    }
}
