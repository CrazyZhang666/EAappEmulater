using BF1ModTools.Utils;
using BF1ModTools.Helper;
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
            NotifierHelper.Success("执行清理Mod数据操作成功");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"清理Mod数据发生异常", ex);
        }
    }

    [RelayCommand]
    private async Task KillBf1Process()
    {
        if (MessageBox.Show("你确定要结束《战地1》进程吗？",
            "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
        {
            await ProcessHelper.CloseProcess(CoreUtil.Name_BF1);
            await ProcessHelper.CloseProcess(CoreUtil.Name_FrostyModManager);
            await ProcessHelper.CloseProcess(CoreUtil.Name_MarneLauncher);
        }
    }

    [RelayCommand]
    private void OpenBF1Folder()
    {
        ProcessHelper.OpenDirectory(Globals.BF1InstallDir);
    }
}
