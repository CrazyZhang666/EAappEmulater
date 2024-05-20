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
    /// </summary>
    private async void Window_Account_Loaded(object sender, RoutedEventArgs e)
    {
        Title = $"战地1模组工具箱 v{CoreUtil.VersionInfo} - {CoreUtil.GetIsAdminStr()}";

        DisplayLoadState("等待玩家操作中...");

        // 读取全局配置文件
        Globals.Read();

        // 仅展示用
        AccountModel.PlayerName = Account.PlayerName;
        // 可被修改
        AccountModel.Remid = Account.Remid;
        AccountModel.Sid = Account.Sid;

        // 战地1路径
        AccountModel.Bf1Path = Globals.BF1AppPath;

        /////////////////////////////////////////////////

        await Task.Run(async () =>
        {
            DisplayLoadState("开始初始化游戏信息...");
            LoggerHelper.Info("开始初始化游戏信息...");

            // 关闭服务进程
            await CoreUtil.CloseServiceProcess();

            // 清理数据文件
            FileHelper.ClearDirectory(CoreUtil.Dir_AppData);

            DisplayLoadState("正在释放资源服务进程文件...");
            LoggerHelper.Info("正在释放资源服务进程文件...");
            FileHelper.ExtractResFile("Exec.EADesktop.exe", CoreUtil.File_Service_EADesktop);
            FileHelper.ExtractResFile("Exec.OriginDebug.exe", CoreUtil.File_Service_OriginDebug);
            FileHelper.ExtractResFile("Exec.BF1Chat.exe", CoreUtil.File_Service_BF1Chat);
            DisplayLoadState("释放资源服务进程文件成功");
            LoggerHelper.Info("释放资源服务进程文件成功");

            DisplayLoadState("正在释放资源数据文件...");
            LoggerHelper.Info("正在释放资源数据文件...");

            FileHelper.ExtractResFile("Exec.AppData.zip", CoreUtil.File_AppData);
            ZipFile.ExtractToDirectory(CoreUtil.File_AppData, CoreUtil.Dir_AppData, true);

            DisplayLoadState("释放资源数据文件成功");
            LoggerHelper.Info("释放资源数据文件成功");

            DisplayLoadState("初始化游戏信息成功");
            LoggerHelper.Info("初始化游戏信息成功");
        });
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
        SaveData();
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

        Globals.Write();
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

    /// <summary>
    /// 显示加载状态到UI界面
    /// </summary>
    private void DisplayLoadState(string log)
    {
        AccountModel.CheckState = log;
    }

    /// <summary>
    /// 选择战地1文件路径
    /// </summary>
    [RelayCommand]
    private void SelcetBf1Path()
    {
        // 战地1路径无效，重新选择
        var dialog = new OpenFileDialog
        {
            Title = "请选择战地1游戏主程序 bf1.exe 文件路径",
            FileName = "bf1.exe",
            DefaultExt = ".exe",
            Filter = "可执行文件 (.exe)|*.exe",
            Multiselect = false,
            InitialDirectory = Globals.DialogDir,
            RestoreDirectory = true,
            AddExtension = true,
            CheckFileExists = true,
            CheckPathExists = true
        };

        // 如果未选择，则退出程序
        if (dialog.ShowDialog() == false)
            return;

        var dirPath = Path.GetDirectoryName(dialog.FileName);
        // 记住本次选择的文件路径
        Globals.DialogDir = dirPath;

        // 开始校验文件有效性
        if (!CoreUtil.IsBf1MainAppFile(dialog.FileName))
        {
            LoggerHelper.Warn($"战地1游戏主程序路径无效，请重新选择 {dialog.FileName}");
            DisplayLoadState($"战地1游戏主程序路径无效，请重新选择");
            return;
        }

        // 检查战地1所在目录磁盘格式
        var diskFlag = Path.GetPathRoot(dirPath);
        var driveInfo = new DriveInfo(diskFlag);
        if (!driveInfo.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase))
        {
            LoggerHelper.Info($"检测到战地1所在磁盘格式不是NTFS，请转换磁盘格式 {Globals.BF1AppPath}");
            DisplayLoadState("检测到战地1所在磁盘格式不是NTFS，请转换磁盘格式");
            return;
        }

        Globals.SetBF1AppPath(dialog.FileName);
        AccountModel.Bf1Path = dialog.FileName;
        LoggerHelper.Info($"获取战地1游戏主程序路径成功 {dialog.FileName}");
        DisplayLoadState("获取战地1游戏主程序路径成功");
        return;
    }
}
