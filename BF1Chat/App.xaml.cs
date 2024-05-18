namespace BF1Chat;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 主程序互斥体
    /// </summary>
    public static Mutex AppMainMutex;
    /// <summary>
    /// 应用程序名称
    /// </summary>
    private readonly string AppName = ResourceAssembly.GetName().Name;

    /// <summary>
    /// 保证程序只能同时启动一个
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        AppMainMutex = new Mutex(true, AppName, out var createdNew);
        if (!createdNew)
        {
            Current.Shutdown();
            return;
        }

        base.OnStartup(e);
    }
}
