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

    // 是否显示真实 Cookie
    private bool _showCookie;
    public bool ShowCookie
    {
        get => _showCookie;
        set { _showCookie = value; OnPropertyChanged(nameof(ShowCookie)); }
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

        // 动态扫描已有的 Account*.ini 文件
        LoadExistingAccounts();

        // 读取全局配置文件
        Globals.Read();
        // 设置上次选中配置槽
        ListBox_AccountInfo.SelectedIndex = (int)Globals.AccountSlot;
    }

    private void LoadExistingAccounts()
    {
        ObsCol_AccountInfos.Clear();

        // 扫描目录
        if (!Directory.Exists(CoreUtil.Dir_Account))
            Directory.CreateDirectory(CoreUtil.Dir_Account);

        var files = Directory.GetFiles(CoreUtil.Dir_Account, "Account*.ini").OrderBy(f => f).ToList();
        // if none, create one
        if (files.Count == 0)
        {
            var path = Path.Combine(CoreUtil.Dir_Account, "Account0.ini");
            FileHelper.CreateFile(path);
            files.Add(path);
        }

        Account.AccountPathDb.Clear();
        int slotIndex = 0;
        foreach (var file in files)
        {
            // 忽略不符合命名规范的文件
            var fileName = Path.GetFileName(file);
            if (!fileName.StartsWith("Account", StringComparison.OrdinalIgnoreCase) || !fileName.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                continue;

            var slot = (AccountSlot)slotIndex;
            Account.AccountPathDb[slot] = file;

            var account = new AccountInfo()
            {
                AccountSlot = slot,
                PlayerName = IniHelper.ReadString("Account", "PlayerName", file),
                AvatarId = IniHelper.ReadString("Account", "AvatarId", file),
                Avatar = IniHelper.ReadString("Account", "Avatar", file),
                Remid = IniHelper.ReadString("Cookie", "Remid", file),
                Sid = IniHelper.ReadString("Cookie", "Sid", file)
            };
            // 玩家头像为空处理（仅有Cookie数据）
            if (!string.IsNullOrWhiteSpace(account.Remid) && string.IsNullOrWhiteSpace(account.Avatar))
                account.Avatar = "Default";
            ObsCol_AccountInfos.Add(account);
            // ensure cache directory mapping set
            CoreUtil.GetAccountCacheDir(slot);
            slotIndex++;
        }
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

    /// <summary>
    /// 复制 Remid
    /// </summary>
    [RelayCommand]
    private void CopyRemid()
    {
        if (ListBox_AccountInfo.SelectedItem is AccountInfo account && !string.IsNullOrWhiteSpace(account.Remid))
        {
            Clipboard.SetText(account.Remid);
        }
    }

    /// <summary>
    /// 复制 Sid
    /// </summary>
    [RelayCommand]
    private void CopySid()
    {
        if (ListBox_AccountInfo.SelectedItem is AccountInfo account && !string.IsNullOrWhiteSpace(account.Sid))
        {
            Clipboard.SetText(account.Sid);
        }
    }

    /// <summary>
    /// 添加账号
    /// </summary>
    [RelayCommand]
    private void AddAccount()
    {
        int nextIndex = ObsCol_AccountInfos.Count;
        if (nextIndex >= 100) return; // 保留上限

        var slot = (AccountSlot)nextIndex;
        // 创建配置文件与缓存目录
        var path = Account.EnsureIniForSlot(slot);
        CoreUtil.GetAccountCacheDir(slot);

        var info = new AccountInfo { AccountSlot = slot, PlayerName = string.Empty, AvatarId = string.Empty, Avatar = string.Empty, Remid = string.Empty, Sid = string.Empty };
        ObsCol_AccountInfos.Add(info);
        ListBox_AccountInfo.SelectedItem = info;

        // 确保选中新增账号
        Globals.AccountSlot = slot;
        Globals.Write();
    }

    /// <summary>
    /// 删除账号
    /// </summary>
    [RelayCommand]
    private void DeleteAccount()
    {
        if (ListBox_AccountInfo.SelectedItem is not AccountInfo account) return;
        if (ObsCol_AccountInfos.Count <= 1) return;

        var removedIndex = (int)account.AccountSlot;

        // 删除对应 ini 文件 与 cache 文件夹
        try
        {
            var path = Account.AccountPathDb[account.AccountSlot];
            if (File.Exists(path)) File.Delete(path);
        }
        catch { }

        try
        {
            var cacheDir = Path.Combine(CoreUtil.Dir_Cache, $"Account{removedIndex}");
            if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, true);
        }
        catch { }

        // 移除集合
        ObsCol_AccountInfos.Remove(account);

        // 重新整理索引与路径映射：重命名剩余文件到连续编号（Account0.ini..）并更新映射
        var remaining = ObsCol_AccountInfos.ToList();
        Account.AccountPathDb.Clear();
        for (int i = 0; i < remaining.Count; i++)
        {
            var info = remaining[i];
            var desiredPath = Path.Combine(CoreUtil.Dir_Account, $"Account{i}.ini");

            // 如果当前路径不是期望路径，则重命名
            var currentPathCandidates = Directory.GetFiles(CoreUtil.Dir_Account, $"Account*.ini").OrderBy(f => f).ToList();
            string currentPath = currentPathCandidates.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"Account{(int)info.AccountSlot}", StringComparison.OrdinalIgnoreCase)) ?? desiredPath;

            if (!string.Equals(currentPath, desiredPath, StringComparison.OrdinalIgnoreCase))
            {
                try { if (File.Exists(currentPath)) File.Move(currentPath, desiredPath, true); } catch { FileHelper.CreateFile(desiredPath); }
            }
            else
            {
                FileHelper.CreateFile(desiredPath);
            }

            // 移动缓存目录
            var oldCache = Path.Combine(CoreUtil.Dir_Cache, $"Account{(int)info.AccountSlot}");
            var newCache = Path.Combine(CoreUtil.Dir_Cache, $"Account{i}");
            try
            {
                if (Directory.Exists(oldCache) && !Directory.Exists(newCache))
                    Directory.Move(oldCache, newCache);
                else
                    FileHelper.CreateDirectory(newCache);
            }
            catch { FileHelper.CreateDirectory(newCache); }

            info.AccountSlot = (AccountSlot)i;
            Account.AccountPathDb[info.AccountSlot] = desiredPath;
        }

        // 选中最后一个可用
        if (ObsCol_AccountInfos.Count > 0)
        {
            ListBox_AccountInfo.SelectedIndex = ObsCol_AccountInfos.Count - 1;
            Globals.AccountSlot = ObsCol_AccountInfos.Last().AccountSlot;
        }
        else
        {
            ListBox_AccountInfo.SelectedIndex = 0;
            Globals.AccountSlot = AccountSlot.S0;
        }

        Globals.Write();
    }
}
