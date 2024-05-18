namespace BF1ModTools.Native;

public static class Chat
{
    /// <summary>
    /// 聊天框消息起始偏移
    /// </summary>
    public const int OFFSET_CHAT_MESSAGE_START = 0x180;
    /// <summary>
    /// 聊天框消息结束偏移
    /// </summary>
    public const int OFFSET_CHAT_MESSAGE_END = 0x188;

    /// <summary>
    /// 申请的内存地址
    /// </summary>
    public static IntPtr AllocateMemAddress { get; private set; } = IntPtr.Zero;

    /// <summary>
    /// 获取战地1聊天框是否开启
    /// </summary>
    /// <returns></returns>
    public static bool IsBf1ChatOpen()
    {
        if (!Memory.IsValid(Memory.Bf1ProBaseAddress))
            return false;

        var address = Memory.Read<long>(Memory.Bf1ProBaseAddress + 0x39F1E50);
        if (!Memory.IsValid(address))
            return false;
        address = Memory.Read<long>(address + 0x08);
        if (!Memory.IsValid(address))
            return false;
        address = Memory.Read<long>(address + 0x28);
        if (!Memory.IsValid(address))
            return false;
        address = Memory.Read<long>(address + 0x00);
        if (!Memory.IsValid(address))
            return false;
        address = Memory.Read<long>(address + 0x20);
        if (!Memory.IsValid(address))
            return false;
        address = Memory.Read<long>(address + 0x18);
        if (!Memory.IsValid(address))
            return false;
        address = Memory.Read<long>(address + 0x28);
        if (!Memory.IsValid(address))
            return false;
        address = Memory.Read<long>(address + 0x38);
        if (!Memory.IsValid(address))
            return false;
        address = Memory.Read<long>(address + 0x40);
        if (!Memory.IsValid(address))
            return false;

        return Memory.Read<byte>(address + 0x30) == 0x01;
    }

    /// <summary>
    /// 获取聊天框指针地址
    /// </summary>
    /// <returns></returns>
    public static long GetChatMessagePtr()
    {
        if (!Memory.IsValid(Memory.Bf1ProBaseAddress))
            return 0;

        var address = Memory.Read<long>(Memory.Bf1ProBaseAddress + 0x3A327E0);
        if (!Memory.IsValid(address))
            return 0;
        address = Memory.Read<long>(address + 0x20);
        if (!Memory.IsValid(address))
            return 0;
        address = Memory.Read<long>(address + 0x18);
        if (!Memory.IsValid(address))
            return 0;
        address = Memory.Read<long>(address + 0x38);
        if (!Memory.IsValid(address))
            return 0;
        address = Memory.Read<long>(address + 0x08);
        if (!Memory.IsValid(address))
            return 0;
        address = Memory.Read<long>(address + 0x68);
        if (!Memory.IsValid(address))
            return 0;
        address = Memory.Read<long>(address + 0xB8);
        if (!Memory.IsValid(address))
            return 0;
        address = Memory.Read<long>(address + 0x10);
        if (!Memory.IsValid(address))
            return 0;
        address = Memory.Read<long>(address + 0x10);
        if (!Memory.IsValid(address))
            return 0;
        else
            return address;
    }

    /// <summary>
    /// 判断战地1窗口是否在最前
    /// </summary>
    /// <returns></returns>
    public static bool IsBf1WindowTopmost()
    {
        var address = Memory.Read<long>(Offsets.OFFSET_DXRENDERER);
        if (!Memory.IsValid(address))
            return false;
        address = Memory.Read<long>(address + 0x820);
        if (!Memory.IsValid(address))
            return false;

        return Memory.Read<byte>(address + 0x5F) == 0x01;
    }

    /// <summary>
    /// 判断战地1窗口是否全屏
    /// </summary>
    public static bool IsWindowFullscreen()
    {
        if (Marshal.GetExceptionForHR(Win32.SHQueryUserNotificationState(out UserNotificationState state)) == null &&
            state == UserNotificationState.QUNS_RUNNING_D3D_FULL_SCREEN)
        {
            return true;
        }

        return false;
    }

    //////////////////////////////////////////////////////////////////

    /// <summary>
    /// 发送聊天消息到游戏
    /// </summary>
    /// <param name="message"></param>
    public static async Task SendChatMsgToGame(string message)
    {
        // 去除多余空格
        message = message.Trim();

        // 如果聊天窗口未开启，则退出
        if (!IsBf1ChatOpen())
            return;

        // 挂起战地1进程
        Memory.SuspendProcess();

        // 获取聊天消息长度
        var msgLength = Encoding.UTF8.GetBytes(message).Length;
        // 写入聊天消息到申请的内存中
        Memory.WriteString(AllocateMemAddress.ToInt64(), message);

        var chatMsgPtr = GetChatMessagePtr();
        var startPtr = chatMsgPtr + OFFSET_CHAT_MESSAGE_START;
        var endPtr = chatMsgPtr + OFFSET_CHAT_MESSAGE_END;

        var oldStartPtr = Memory.Read<long>(startPtr);
        var oldEndPtr = Memory.Read<long>(endPtr);

        Memory.Write(startPtr, AllocateMemAddress.ToInt64());
        Memory.Write(endPtr, AllocateMemAddress.ToInt64() + msgLength);

        // 恢复战地1进程
        Memory.ResumeProcess();

        //////////////////////////////////////////////////////

        await Memory.KeyPress(WinVK.RETURN);

        //////////////////////////////////////////////////////

        // 挂起战地1进程
        Memory.SuspendProcess();

        Memory.Write(startPtr, oldStartPtr);
        Memory.Write(endPtr, oldEndPtr);

        // 恢复战地1进程
        Memory.ResumeProcess();
    }

    /// <summary>
    /// 判断战地1输入框字符串长度，中文3，英文1
    /// </summary>
    /// <param name="str">需要判断的字符串</param>
    /// <returns></returns>
    public static int GetStrLength(string str)
    {
        str = str.Trim();
        if (string.IsNullOrEmpty(str))
            return 0;

        int tempLen = 0;
        var bytes = new ASCIIEncoding().GetBytes(str);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 63)
                tempLen += 3;
            else
                tempLen += 1;
        }

        return tempLen;
    }

    //////////////////////////////////////////////////////////////////

    /// <summary>
    /// 申请内存空间
    /// </summary>
    /// <returns></returns>
    public static bool AllocateMemory()
    {
        if (AllocateMemAddress == IntPtr.Zero)
            AllocateMemAddress = Win32.VirtualAllocEx(Memory.Bf1ProHandle, IntPtr.Zero, (IntPtr)0x300, AllocationType.Commit, MemoryProtection.ReadWrite);

        return AllocateMemAddress != IntPtr.Zero;
    }

    /// <summary>
    /// 释放申请的内存空间
    /// </summary>
    public static void FreeMemory()
    {
        if (AllocateMemAddress != IntPtr.Zero)
            Win32.VirtualFreeEx(Memory.Bf1ProHandle, AllocateMemAddress, 0, AllocationType.Reset);
    }
}
