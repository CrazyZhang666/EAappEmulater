using BF1ModTools.Utils;
using BF1ModTools.Helper;
using BF1ModTools.Native;
using CommunityToolkit.Mvvm.Input;

namespace BF1ModTools.Windows;

/// <summary>
/// ChatWindow.xaml 的交互逻辑
/// </summary>
public partial class ChatWindow : Window
{
    private IntPtr ThisWindowHandle = IntPtr.Zero;

    private bool _isAppRunning = true;
    private bool _isToggleChsIME = true;

    public ChatWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成线程
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 获取当前窗口句柄
        ThisWindowHandle = new WindowInteropHelper(this).Handle;

        // 隐藏窗口
        this.Hide();

        ///////////////////////////

        // 更新战地1初始化线程
        new Thread(UpdateBf1InitThread)
        {
            Name = "UpdateBf1InitThread",
            IsBackground = true
        }.Start();

        // 更新战地1输入状态线程
        new Thread(UpdateInputStateThread)
        {
            Name = "UpdateInputStateThread",
            IsBackground = true
        }.Start();
    }

    /// <summary>
    /// 窗口加载关闭时线程
    /// </summary>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _isAppRunning = false;
    }

    /// <summary>
    /// 窗口失去焦点时
    /// </summary>
    private void Window_Deactivated(object sender, EventArgs e)
    {
        HideWindow();
    }

    /// <summary>
    /// 更新战地1初始化线程
    /// </summary>
    private void UpdateBf1InitThread()
    {
        var winNameList = new List<string>();

        while (_isAppRunning)
        {
            // 防止程序关闭时，此窗口未关闭
            this.Dispatcher.Invoke(() =>
            {
                winNameList.Clear();
                foreach (Window Window in Application.Current.Windows)
                {
                    winNameList.Add(Window.Name);
                }

                // 当主窗口都不存在的时候，退出程序
                if (!winNameList.Contains("Window_Account") &&
                    !winNameList.Contains("Window_Load") &&
                    !winNameList.Contains("Window_Login") &&
                    !winNameList.Contains("Window_Main"))
                {
                    _isAppRunning = false;
                    Application.Current.Shutdown();
                    return;
                }
            });

            // 判断战地1是否在运行
            if (ProcessHelper.IsAppRun("bf1"))
            {
                // 如果战地在运行
                // 如果战地1进程句柄为空，则代表战地1内存未初始化
                if (Memory.Bf1ProHandle == IntPtr.Zero)
                {
                    // 尝试初始化战地1内存
                    if (Memory.Initialize())
                    {
                        Chat.AllocateMemory();
                        LoggerHelper.Info($"战地1聊天指针分配成功 0x{Chat.AllocateMemAddress:x}");
                    }
                }
            }
            else
            {
                // 如果战地未运行
                // 如果战地1进程句柄不为空，则代表战地1内存已经初始化，但是未释放
                if (Memory.Bf1ProHandle != IntPtr.Zero)
                {
                    // 释放战地1聊天资源
                    Chat.FreeMemory();
                    LoggerHelper.Info("释放战地1聊天指针内存成功");

                    // 释放战地1内存资源
                    Memory.UnInitialize();
                    LoggerHelper.Info("释放战地1内存模块资源成功");
                }
            }

            Thread.Sleep(1000);
        }
    }

    /// <summary>
    /// 更新输入状态线程
    /// </summary>
    private void UpdateInputStateThread()
    {
        // 避免窗口重复显示
        bool isShow = false;

        while (_isAppRunning)
        {
            // 当聊天框是开启
            if (Chat.IsBf1ChatOpen())
            {
                // 聊天框开启
                // 如果输入窗口不显示
                // 并且 必须是非全屏状态
                if (!isShow && !Chat.IsWindowFullscreen())
                {
                    isShow = true;

                    this.Dispatcher.Invoke(() =>
                    {
                        var thisWinThreadId = Win32.GetWindowThreadProcessId(ThisWindowHandle, IntPtr.Zero);
                        var currForeWindow = Win32.GetForegroundWindow();
                        var currForeWinThreadId = Win32.GetWindowThreadProcessId(currForeWindow, IntPtr.Zero);

                        Win32.AttachThreadInput(currForeWinThreadId, thisWinThreadId, true);

                        this.Show();
                        this.Activate();
                        this.Focus();

                        Win32.AttachThreadInput(currForeWinThreadId, thisWinThreadId, false);

                        this.Topmost = true;
                        this.Topmost = false;

                        _ = Win32.SetForegroundWindow(ThisWindowHandle);
                        TextBox_InputMessage.Focus();

                        // 获取战地1窗口数据
                        if (Memory.GetWindowData(out WindowData windowData))
                        {
                            // 设置当前聊天窗口大小和位置
                            this.Top = windowData.Top + 10;
                            this.Left = windowData.Left + 10;
                            this.Width = windowData.Width - 20;
                        }

                        ChatUtil.SetInputLangZHCN();
                    });
                }
            }
            else
            {
                // 聊天框关闭
                // 如果输入窗口显示
                if (isShow)
                {
                    isShow = false;

                    this.Dispatcher.Invoke(HideWindow);
                }
            }

            Thread.Sleep(200);
        }
    }

    private void TextBlock_InputMethod_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isToggleChsIME = !_isToggleChsIME;

        if (_isToggleChsIME)
        {
            ChatUtil.SetInputLangZHCN();
            TextBlock_InputMethod.Text = "中";
        }
        else
        {
            ChatUtil.SetInputLangENUS();
            TextBlock_InputMethod.Text = "英";
        }
    }

    /// <summary>
    /// 发送聊天消息
    /// </summary>
    [RelayCommand]
    private void SendChatMessage()
    {
        var message = TextBox_InputMessage.Text.Trim();

        TextBox_InputMessage.Clear();

        ChatUtil.SetInputLangENUS();
        Memory.SetBF1WindowForeground();

        Chat.SendChatMsgToGame(message);
    }

    /// <summary>
    /// 取消发送消息
    /// </summary>
    [RelayCommand]
    private void CancelChatMessage()
    {
        HideWindow();
        Memory.KeyPress(WinVK.ESCAPE);
    }

    /// <summary>
    /// 隐藏输入窗口
    /// </summary>
    private void HideWindow()
    {
        TextBox_InputMessage.Clear();
        ChatUtil.SetInputLangENUS();
        this.Hide();
    }
}
