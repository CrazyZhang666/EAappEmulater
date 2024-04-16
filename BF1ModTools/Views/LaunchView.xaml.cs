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
    }

    #region Frosty Mod Manager
    [RelayCommand]
    private void SelectFbmodFiles()
    {
        // 如果战地1正在运行，则不允许启动FrostyModManager
        if (ProcessHelper.IsAppRun(CoreUtil.Name_BF1))
        {
            NotifierHelper.Warning("战地1正在运行，请关闭后再启动本程序");
            return;
        }

        // 如果程序已经在运行，则结束操作
        if (ProcessHelper.IsAppRun("FrostyModManager"))
        {
            NotifierHelper.Warning("程序已经运行了，请不要重复运行");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "请选择要安装的寒霜Mod文件（支持多选）",
            DefaultExt = ".fbmod",
            Filter = "寒霜Mod文件 (.fbmod)|*.fbmod",
            Multiselect = true,
            InitialDirectory = Globals.DialogDir2,
            RestoreDirectory = true,
            AddExtension = true,
            CheckFileExists = true,
            CheckPathExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            Globals.DialogDir2 = Path.GetDirectoryName(dialog.FileName);

            try
            {
                // 清空旧版Mod文件夹
                FileHelper.ClearDirectory(CoreUtil.Dir_FrostyMod_Mods_Bf1);
                LoggerHelper.Info("清空旧版 Mod 文件夹 成功");

                // 创建FrostyMod配置文件
                var modConfig = new ModConfig();

                // 设置战地1安装目录
                modConfig.Games.bf1.GamePath = Globals.BF1InstallDir;

                LoggerHelper.Info($"当前选中 Mod 数量: {dialog.FileNames.Length}");

                var modFiles = new List<string>();
                foreach (var file in dialog.FileNames)
                {
                    var fileName = Path.GetFileName(file);

                    // 复制mod文件到寒霜mod管理器特定文件夹
                    File.Copy(file, Path.Combine(CoreUtil.Dir_FrostyMod_Mods_Bf1, fileName));
                    modFiles.Add($"{fileName}:True");

                    LoggerHelper.Info($"已选择 Mod 名称: {fileName}");
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
    }

    [RelayCommand]
    private void RunFrostyModManager()
    {
        ProcessHelper.OpenProcess(CoreUtil.File_FrostyMod_FrostyModManager);
    }

    [RelayCommand]
    private async Task CloseFrostyModManager()
    {
        await ProcessHelper.CloseProcess(CoreUtil.Name_FrostyModManager);
    }
    #endregion

    #region BF1 Marne Launcher
    [RelayCommand]
    private void RunMarneLauncher()
    {
        ProcessHelper.OpenProcess(CoreUtil.File_Marne_MarneLauncher);
    }

    [RelayCommand]
    private async Task CloseMarneLauncher()
    {
        await ProcessHelper.CloseProcess(CoreUtil.Name_MarneLauncher);
    }
    #endregion
}
