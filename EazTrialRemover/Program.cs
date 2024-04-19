using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EazTrialRemover;

internal class Program
{
    static void Main(string[] args)
    {
        Console.Title = "Eazfuscator.NET 试用7天移除限制工具";
        LoggerHelper.Info($"欢迎使用 {Console.Title}");

        if (args.Length == 0)
        {
            LoggerHelper.Warn("未发现运行参数！程序结束");
            return;
        }

        var path = args[0];
        LoggerHelper.Trace($"传入文件路径 {path}");

        if (!path.EndsWith("dll"))
        {
            LoggerHelper.Warn("请输入dll文件！程序结束");
            return;
        }

        if (!File.Exists(path))
        {
            LoggerHelper.Warn("输入dll文件不存在！程序结束");
            return;
        }

        Patch(path);

        LoggerHelper.Trace("检查 Patch 修改结果");
        Patch(path, false);

        LoggerHelper.Trace("试用7天移除限制执行完毕！程序结束");
    }

    static void Patch(string path, bool isSave = true)
    {
        LoggerHelper.Trace("开始查找检查试用类...");

        var moduleDef = ModuleDefMD.Load(path);
        var typeDefs = moduleDef.GetTypes().ToList();

        foreach (var typeDef in typeDefs)
        {
            if (!IsTrialCheckClass(typeDef))
                continue;

            LoggerHelper.Trace($"发现检查试用类: 0x{typeDef.MDToken}");

            foreach (var method in typeDef.Methods)
            {
                if (!method.HasBody)
                    continue;

                LoggerHelper.Trace($"{method.FullName}");
                LoggerHelper.Trace($"Body.Instructions {method.Body.Instructions.Count}");
                LoggerHelper.Trace($"Body.ExceptionHandlers {method.Body.ExceptionHandlers.Count}");

                var insts = method.Body.Instructions;

                insts.Clear();
                method.Body.ExceptionHandlers.Clear();

                if (MethodRets(method, "Boolean"))
                    insts.Add(OpCodes.Ldc_I4_1.ToInstruction());

                insts.Add(OpCodes.Ret.ToInstruction());
            }

            if (isSave)
                Save(moduleDef, path);

            return;
        }
    }

    static bool IsTrialCheckClass(TypeDef type)
    {
        if (!type.HasMethods)
            return false;

        var methods = type.Methods;

        if (methods.Count != 4)
            return false;

        for (int i = 0; i < 4; i++)
        {
            if (i < 2)
            {
                if (!MethodRets(methods[i], "Boolean"))
                    return false;
            }
            else
            {
                if (!MethodRets(methods[i]))
                    return false;
            }
        }

        return true;
    }

    static bool MethodRets(MethodDef methodDef, string retType = null)
    {
        if (!methodDef.HasReturnType)
            return string.IsNullOrEmpty(retType) || retType == "Void";

        return $"System.{retType}" == methodDef.ReturnType.FullName;
    }

    static void Save(ModuleDefMD moduleDef, string path)
    {
        LoggerHelper.Trace("开始保存修补过的文件...");

        var backupFile = path.Replace(".dll", "_backup.dll");

        if (File.Exists(backupFile))
            File.Delete(backupFile);

        File.Move(path, backupFile);

        moduleDef.Write(path, new(moduleDef)
        {
            AddCheckSum = true
        });

        moduleDef.Dispose();

        File.Delete(backupFile);

        LoggerHelper.Trace("保存修补过的文件成功");
    }
}
