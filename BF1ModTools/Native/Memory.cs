namespace BF1ModTools.Native;

public static class Memory
{
    /// <summary>
    /// 战地1进程类
    /// </summary>
    private static Process Bf1Process { get; set; } = null;
    /// <summary>
    /// 战地1窗口句柄
    /// </summary>
    public static IntPtr Bf1WinHandle { get; private set; } = IntPtr.Zero;
    /// <summary>
    /// 战地1进程Id
    /// </summary>
    public static int Bf1ProId { get; private set; } = 0;
    /// <summary>
    /// 战地1主模块基址
    /// </summary>
    public static long Bf1ProBaseAddress { get; private set; } = 0;
    /// <summary>
    /// 战地1进程句柄
    /// </summary>
    public static IntPtr Bf1ProHandle { get; private set; } = IntPtr.Zero;

    public static bool Initialize()
    {
        try
        {
            var pArray = Process.GetProcessesByName("bf1");
            if (pArray.Length <= 0)
                return false;

            foreach (var item in pArray)
            {
                if (item.MainWindowTitle.Equals("Battlefield™ 1"))
                {
                    Bf1Process = item;
                    break;
                }
            }

            if (Bf1Process == null)
                return false;
            if (Bf1Process.MainModule == null)
                return false;

            Bf1WinHandle = Bf1Process.MainWindowHandle;
            Bf1ProId = Bf1Process.Id;

            Bf1ProHandle = Win32.OpenProcess(ProcessAccessFlags.All, false, Bf1ProId);
            Bf1ProBaseAddress = Bf1Process.MainModule.BaseAddress.ToInt64();

            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// 释放内存模块
    /// </summary>
    public static void UnInitialize()
    {
        if (Bf1ProHandle != IntPtr.Zero)
        {
            Win32.CloseHandle(Bf1ProHandle);
            Bf1ProHandle = IntPtr.Zero;
        }

        if (Bf1Process != null)
            Bf1Process = null;

        if (Bf1WinHandle != IntPtr.Zero)
            Bf1WinHandle = IntPtr.Zero;

        if (Bf1ProId != 0)
            Bf1ProId = 0;

        if (Bf1ProBaseAddress != 0)
            Bf1ProBaseAddress = 0;
    }

    /// <summary>
    /// 将战地1窗口置于前面
    /// </summary>
    public static void SetBF1WindowForeground()
    {
        _ = Win32.SetForegroundWindow(Bf1WinHandle);
    }

    /// <summary>
    /// 按键模拟
    /// </summary>
    public static void KeyPress(WinVK winVK, int delay = 50)
    {
        Win32.Keybd_Event(winVK, Win32.MapVirtualKey(winVK, 0), 0, 0);
        Thread.Sleep(delay);
        Win32.Keybd_Event(winVK, Win32.MapVirtualKey(winVK, 0), 2, 0);
        Thread.Sleep(delay);
    }

    /// <summary>
    /// 暂停战地1进程
    /// </summary>
    public static void SuspendProcess()
    {
        Win32.NtSuspendProcess(Bf1ProHandle);
    }

    /// <summary>
    /// 恢复进程
    /// </summary>
    public static void ResumeProcess()
    {
        Win32.NtResumeProcess(Bf1ProHandle);
    }

    /// <summary>
    /// 获取战地1窗口数据
    /// </summary>
    public static bool GetWindowData(out WindowData windowData)
    {
        // 获取指定窗口句柄的窗口矩形数据和客户区矩形数据
        Win32.GetWindowRect(Bf1WinHandle, out RECT windowRect);
        Win32.GetClientRect(Bf1WinHandle, out RECT clientRect);

        // 计算窗口区的宽和高
        var windowWidth = windowRect.Right - windowRect.Left;
        var windowHeight = windowRect.Bottom - windowRect.Top;

        // 处理窗口最小化
        if (windowRect.Left == -32000)
        {
            windowData.Left = 0;
            windowData.Top = 0;
            windowData.Width = 1;
            windowData.Height = 1;
            return false;
        }

        // 计算客户区的宽和高
        var clientWidth = clientRect.Right - clientRect.Left;
        var clientHeight = clientRect.Bottom - clientRect.Top;

        // 计算边框
        var borderWidth = (windowWidth - clientWidth) / 2;
        var borderHeight = windowHeight - clientHeight - borderWidth;

        windowData.Left = windowRect.Left += borderWidth;
        windowData.Top = windowRect.Top += borderHeight;
        windowData.Width = clientWidth;
        windowData.Height = clientHeight;
        return true;
    }

    //////////////////////////////////////////////////////////////////

    /// <summary>
    /// 泛型读取内存
    /// </summary>
    public static T Read<T>(long address) where T : struct
    {
        var buffer = new byte[Marshal.SizeOf(typeof(T))];
        Win32.ReadProcessMemory(Bf1ProHandle, address, buffer, buffer.Length, out _);
        return ByteArrayToStructure<T>(buffer);
    }

    /// <summary>
    /// 泛型写入内存
    /// </summary>
    public static void Write<T>(long address, T value) where T : struct
    {
        var buffer = StructureToByteArray(value);
        Win32.WriteProcessMemory(Bf1ProHandle, address, buffer, buffer.Length, out _);
    }

    /// <summary>
    /// 写入字符串
    /// </summary>
    public static void WriteString(long address, string vaule)
    {
        var buffer = new UTF8Encoding().GetBytes(vaule);
        Win32.WriteProcessMemory(Bf1ProHandle, address, buffer, buffer.Length, out _);
    }

    //////////////////////////////////////////////////////////////////

    /// <summary>
    /// 判断内存地址是否有效
    /// </summary>
    /// <param name="Address"></param>
    /// <returns></returns>
    public static bool IsValid(long Address)
    {
        return Address >= 0x10000 && Address < 0x000F000000000000;
    }

    /// <summary>
    /// 字符数组转结构
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var obj = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            if (obj != null)
                return (T)obj;
            else
                return default;
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// 结构转字节数组
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private static byte[] StructureToByteArray(object obj)
    {
        var length = Marshal.SizeOf(obj);
        var array = new byte[length];
        IntPtr pointer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(obj, pointer, true);
        Marshal.Copy(pointer, array, 0, length);
        Marshal.FreeHGlobal(pointer);
        return array;
    }
}

public struct WindowData
{
    public int Left;
    public int Top;
    public int Width;
    public int Height;
}
