using BF1ModTools.Utils;
using BF1ModTools.Helper;
using BF1ModTools.Models;
using CommunityToolkit.Mvvm.Input;

namespace BF1ModTools.Views;

/// <summary>
/// ToolsView.xaml 的交互逻辑
/// </summary>
public partial class ToolsView : UserControl
{
    public ToolsView()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void CheckModState()
    {
        try
        {
            if (!ProcessHelper.IsAppRun(CoreUtil.Name_BF1))
            {
                NotifierHelper.Warning("战地1未运行，请先Mod启动战地1再执行本操作");
                return;
            }

            var jsonPath = Path.Combine(Globals.BF1InstallDir, "ModData\\Default\\patch\\mods.json");
            if (!File.Exists(jsonPath))
            {
                LoggerHelper.Warn("未发现战地1目录 ModData 里的 mods.json 文件，Mod未生效");
                return;
            }

            var jsonStr = File.ReadAllText(jsonPath);
            var modInfoList = JsonHelper.JsonDeserialize<List<ModInfo>>(jsonStr);
            LoggerHelper.Info($"当前应用Mod数量: {modInfoList.Count}");

            foreach (var modInfo in modInfoList)
            {
                LoggerHelper.Info($"已应用Mod: {modInfo.file_name}");
            }

            var result = ProcessHelper.GetProcessCommandLineArgs(CoreUtil.Name_BF1);
            LoggerHelper.Info($"战地1命令行: {result}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"检测Mod状态发生异常", ex);
        }
    }

    [RelayCommand]
    private void ClearModData()
    {
        try
        {
            if (ProcessHelper.IsAppRun(CoreUtil.Name_BF1))
            {
                NotifierHelper.Warning("战地1正在运行，请关闭后再执行清理Mod数据操作");
                return;
            }

            if (ProcessHelper.IsAppRun(CoreUtil.Name_FrostyModManager))
            {
                NotifierHelper.Warning("FrostyModManager正在运行，请关闭后再执行清理Mod数据操作");
                return;
            }

            var modDataDir = Path.Combine(Globals.BF1InstallDir, "ModData");
            if (!Directory.Exists(modDataDir))
            {
                NotifierHelper.Warning("未发现战地1Mod数据文件夹，操作取消");
                return;
            }

            FileHelper.ClearDirectory(modDataDir);
            NotifierHelper.Success("执行清理Mod数据操作操作成功");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"清理Mod数据发生异常", ex);
        }
    }

    [RelayCommand]
    private async Task KillBf1Process()
    {
        if (MessageBox.Show("你确定要强制结束《战地1》进程吗？",
            "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
        {
            await ProcessHelper.CloseProcess(CoreUtil.Name_BF1);
        }
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        ProcessHelper.OpenDirectory(CoreUtil.Dir_Default);
    }

    [RelayCommand]
    private void OpenBF1Folder()
    {
        ProcessHelper.OpenDirectory(Globals.BF1InstallDir);
    }

    [RelayCommand]
    private void RunBattlefieldChat()
    {
        // 如果程序已经在运行，则结束操作
        if (ProcessHelper.IsAppRun(CoreUtil.Name_BattlefieldChat))
        {
            NotifierHelper.Warning("程序已经运行了，请不要重复运行");
            return;
        }

        ProcessHelper.OpenProcess(CoreUtil.File_BattlefieldChat);
    }
}
