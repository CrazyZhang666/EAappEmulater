using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Core;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using EAappEmulater.Utils;
using EAappEmulater.Api;
using EAappEmulater.Enums;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EAappEmulater.Windows;

/// <summary>
/// AccountWindow.xaml 的交互逻辑
/// </summary>
public partial class AccountWindow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AccountInfo> ObsCol_AccountInfos { get; set; } = new();

    private ObservableCollection<LanguageEntry> _languageList = new();
    public ObservableCollection<LanguageEntry> LanguageList
    {
        get => _languageList;
        set { _languageList = value; OnPropertyChanged(nameof(LanguageList)); }
    }

    private string _currentLanguage = string.Empty;
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value) return;
            _currentLanguage = value;
            OnPropertyChanged(nameof(CurrentLanguage));

            // Apply immediately
            if (!string.IsNullOrWhiteSpace(_currentLanguage))
            {
                try
                {
                    App.SetLanguage(_currentLanguage);
                    Globals.Language = _currentLanguage;
                    Globals.DefaultLanguage = _currentLanguage;
                    Globals.Write();
                }
                catch { }
            }
        }
    }

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public AccountWindow()
    {
        InitializeComponent();

        // load languages
        var langs = LanguageConfigHelper.GetLanguages();
        LanguageList = new ObservableCollection<LanguageEntry>(langs);

        CurrentLanguage = string.IsNullOrWhiteSpace(Globals.Language) ? (Globals.DefaultLanguage ?? "") : Globals.Language;
        if (string.IsNullOrWhiteSpace(CurrentLanguage) && LanguageList.Count > 0)
            CurrentLanguage = LanguageList[0].Code;

        DataContext = this;
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private void Window_Account_Loaded(object sender, RoutedEventArgs e)
    {
        Title = $"EA App 模拟器 v{CoreUtil.VersionInfo}";


        // 遍历读取10个配置文件槽
        foreach (var item in Account.AccountPathDb)
        {
            var account = new AccountInfo()
            {
                // 账号槽
                AccountSlot = item.Key,
                // 仅展示用
                PlayerName = IniHelper.ReadString("Account", "PlayerName", item.Value),
                AvatarId = IniHelper.ReadString("Account", "AvatarId", item.Value),
                Avatar = IniHelper.ReadString("Account", "Avatar", item.Value),
                // 可被修改
                Remid = IniHelper.ReadString("Cookie", "Remid", item.Value),
                Sid = IniHelper.ReadString("Cookie", "Sid", item.Value)
            };

            // 玩家头像为空处理（仅有Cookie数据）
            if (!string.IsNullOrWhiteSpace(account.Remid) && string.IsNullOrWhiteSpace(account.Avatar))
                account.Avatar = "Default";

            // 添加到动态集合中
            ObsCol_AccountInfos.Add(account);
        }

        ////////////////////////////////

        // 读取全局配置文件
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
        SaveAccountCookie();
    }

    /// <summary>
    /// 保存账号Cookie
    /// </summary>
    private bool SaveAccountCookie(bool isReset = false)
    {
        if (ListBox_AccountInfo.SelectedItem is not AccountInfo account)
            return false;

        // 设置当前选择配置槽
        Globals.AccountSlot = account.AccountSlot;

        // 仅更换新账号使用
        if (isReset)
        {
            account.PlayerName = string.Empty;
            account.AvatarId = string.Empty;
            account.Avatar = string.Empty;

            account.Remid = string.Empty;
            account.Sid = string.Empty;
        }

        foreach (var item in ObsCol_AccountInfos)
        {
            var path = Account.AccountPathDb[item.AccountSlot];

            IniHelper.WriteString("Cookie", "Remid", item.Remid, path);
            IniHelper.WriteString("Cookie", "Sid", item.Sid, path);
        }

        // 保存全局配置文件
        Globals.Write();

        return true;
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
    /// 切换语言
    /// </summary>
    [RelayCommand]
    private void ChangeLanguage()
    {
        if (!string.IsNullOrWhiteSpace(CurrentLanguage))
        {
            App.SetLanguage(CurrentLanguage);
            Globals.Language = CurrentLanguage;
            Globals.DefaultLanguage = CurrentLanguage;
            Globals.Write();
        }
    }

    /// <summary>
    /// 登录选中账号
    /// </summary>
    [RelayCommand]
    private async void LoginAccount()
    {
        // 保存数据
        if (!SaveAccountCookie())
            return;

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
    private async void GetCookie()
    {
        // 保存数据
        if (!SaveAccountCookie())
            return;

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
    private async void ChangeAccount()
    {
        // 保存数据
        if (!SaveAccountCookie(true))
            return;

        // 清空当前账号信息
        Account.Reset();
        Account.Write();

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
