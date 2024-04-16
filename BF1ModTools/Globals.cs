using BF1ModTools.Core;
using BF1ModTools.Helper;

namespace BF1ModTools;

public static class Globals
{
    public static void Read()
    {
        LoggerHelper.Info("开始读取配置文件...");

        Account.Read();

        LoggerHelper.Info("读取配置文件成功");
    }

    public static void Write()
    {
        LoggerHelper.Info("开始保存配置文件...");

        Account.Write();

        LoggerHelper.Info("保存配置文件成功");
    }
}
