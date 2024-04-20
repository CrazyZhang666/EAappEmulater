using BF1ModTools.Utils;
using BF1ModTools.Helper;
using BF1ModTools.Models;
using CommunityToolkit.Mvvm.Input;

namespace BF1ModTools.Views;

/// <summary>
/// LaunchView.xaml 的交互逻辑
/// </summary>
public partial class LaunchView : UserControl
{
    public LaunchView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {

    }

    #region Frosty Mod Manager
    /// <summary>
    /// 运行寒霜Mod管理器
    /// </summary>
    [RelayCommand]
    private async Task RunFrostyModManager()
    {
        // 如果战地1正在运行，则不允许启动FrostyModManager
        if (ProcessHelper.IsAppRun(CoreUtil.Name_BF1))
        {
            NotifierHelper.Warning("战地1正在运行，请关闭后再启动本程序");
            return;
        }

        // 如果程序已经在运行，则结束操作
        if (ProcessHelper.IsAppRun(CoreUtil.Name_FrostyModManager))
        {
            NotifierHelper.Warning("程序已经运行了，请不要重复运行");
            return;
        }

        if (!await CoreUtil.GetBf1InstallPath())
            return;

        // 选择要安装的Mod文件（支持多选）
        var dialog = new OpenFileDialog
        {
            Title = "请选择要安装的战地1寒霜Mod文件（支持多选）",
            DefaultExt = ".fbmod",
            Filter = "寒霜Mod文件 (.fbmod)|*.fbmod",
            Multiselect = true,
            InitialDirectory = Globals.DialogDir2,
            RestoreDirectory = true,
            AddExtension = true,
            CheckFileExists = true,
            CheckPathExists = true
        };

        // 如果未选择，则退出程序
        if (dialog.ShowDialog() == false)
            return;

        // 记住本次选择的文件路径
        Globals.DialogDir2 = Path.GetDirectoryName(dialog.FileName);

        // 输出选择Mod文件数量
        LoggerHelper.Info($"当前选择战地1 Mod 数量: {dialog.FileNames.Length}");

        try
        {
            // 文件夹如果不存在则创建
            FileHelper.CreateDirectory(CoreUtil.Dir_FrostyMod_Mods_Bf1);

            // 清空旧版Mod文件夹
            FileHelper.ClearDirectory(CoreUtil.Dir_FrostyMod_Mods_Bf1);
            LoggerHelper.Info("清空旧版 Mod 文件夹 成功");

            // 创建FrostyMod配置文件
            var modConfig = new ModConfig();

            // 设置战地1安装目录
            modConfig.Games.bf1.GamePath = Globals.BF1InstallDir;

            // 临时保存选择Mod文件名称列表
            var modFiles = new List<string>();
            // 遍历选择的Mod文件列表
            foreach (var file in dialog.FileNames)
            {
                // 获取文件名称，带扩展名
                var fileName = Path.GetFileName(file);

                // 复制mod文件到寒霜mod管理器指定文件夹
                File.Copy(file, Path.Combine(CoreUtil.Dir_FrostyMod_Mods_Bf1, fileName), true);
                // 添加Mod文件名全称+特定后缀到列表中
                modFiles.Add($"{fileName}:True");

                LoggerHelper.Info($"已选择战地1 Mod 名称: {fileName}");
            }

            // 设置Mod名称并启用
            modConfig.Games.bf1.Packs.Default = string.Join("|", modFiles);

            // 写入Frosty\manager_config.json配置文件
            File.WriteAllText(CoreUtil.File_FrostyMod_Frosty_ManagerConfig, JsonHelper.JsonSerialize(modConfig));
            LoggerHelper.Info("写入 FrostyModManager 配置文件成功");

            LoggerHelper.Info("正在启动 FrostyModManager 中...");
            ProcessHelper.OpenProcess(CoreUtil.File_FrostyMod_FrostyModManager);
        }
        catch (Exception ex)
        {
            NotifierHelper.Error("安装 Mod 过程中发生异常");
            LoggerHelper.Error($"安装 Mod 过程中发生异常 {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭寒霜Mod管理器
    /// </summary>
    [RelayCommand]
    private async Task CloseFrostyModManager()
    {
        await ProcessHelper.CloseProcess(CoreUtil.Name_FrostyModManager);
    }
    #endregion

    #region BF1 Marne Launcher
    /// <summary>
    /// 运行马恩启动器
    /// </summary>
    [RelayCommand]
    private void RunMarneLauncher()
    {
        // 如果程序已经在运行，则结束操作
        if (ProcessHelper.IsAppRun(CoreUtil.Name_MarneLauncher))
        {
            NotifierHelper.Warning("程序已经运行了，请不要重复运行");
            return;
        }

        ProcessHelper.OpenProcess(CoreUtil.File_Marne_MarneLauncher);
    }

    /// <summary>
    /// 关闭马恩启动器
    /// </summary>
    [RelayCommand]
    private async Task CloseMarneLauncher()
    {
        await ProcessHelper.CloseProcess(CoreUtil.Name_MarneLauncher);
    }
    #endregion
}
