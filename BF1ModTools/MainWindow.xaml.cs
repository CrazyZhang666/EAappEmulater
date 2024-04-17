using BF1ModTools.Core;
using BF1ModTools.Utils;
using BF1ModTools.Helper;

namespace BF1ModTools;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// 用于向外暴露主窗口实例
    /// </summary>
    public static Window MainWindowInstance { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Main_Loaded(object sender, RoutedEventArgs e)
    {
        LoggerHelper.Info("启动主程序成功");

        Title = $"战地1模组工具箱 v{CoreUtil.VersionInfo}";

        // 向外暴露主窗口实例
        MainWindowInstance = this;

        // 初始化工作
        Ready.Run();
    }

    private void Window_Main_Closing(object sender, CancelEventArgs e)
    {
        // 清理工作
        Ready.Stop();

        LoggerHelper.Info("关闭主程序成功");
    }
}