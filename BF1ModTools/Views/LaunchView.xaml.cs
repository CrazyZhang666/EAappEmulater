using BF1ModTools.Utils;
using BF1ModTools.Helper;
using BF1ModTools.Models;
using BF1ModTools.Windows;
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
    private void RunFrostyModManager()
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

        var modWindow = new ModWindow
        {
            Owner = Application.Current.MainWindow
        };
        modWindow.ShowDialog();
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
        // 如果战地1未运行，则不允许启动MarneLauncher
        if (!ProcessHelper.IsAppRun(CoreUtil.Name_BF1))
        {
            NotifierHelper.Warning("战地1未运行，请先启动战地1模组后再执行本操作");
            return;
        }

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
