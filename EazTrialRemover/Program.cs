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
#if DEBUG
        var path = "F:\\Downloads\\EAappEmulater.dll";
#else
        if (args.Length == 0)
        {
            Console.WriteLine("未发现运行参数");
            return;
        }

        var path = args[0];
        Console.WriteLine($"传入文件路径 {path}");
#endif

        if (!path.EndsWith("dll"))
        {
            Console.WriteLine("请输入dll文件");
            return;
        }

        if (!File.Exists(path))
        {
            Console.WriteLine("输入dll文件不存在");
            return;
        }

        Patch(path);

        Console.WriteLine("程序执行完毕！");
    }

    static void Patch(string path)
    {
        var moduleDef = ModuleDefMD.Load(path);
        var typeDefs = moduleDef.GetTypes().ToList();

        foreach (var typeDef in typeDefs)
        {
            if (!IsTrialCheckClass(typeDef))
                continue;

            Console.WriteLine($"发现试用检查类: 0x{typeDef.MDToken}");

            Console.WriteLine();
            foreach (var method in typeDef.Methods)
            {
                if (!method.HasBody)
                    continue;

                Console.WriteLine($"{method.FullName}");
                Console.WriteLine($"Body.Instructions {method.Body.Instructions.Count}");
                Console.WriteLine($"Body.ExceptionHandlers {method.Body.ExceptionHandlers.Count}");
                Console.WriteLine("-----------------");

                var insts = method.Body.Instructions;

                insts.Clear();
                method.Body.ExceptionHandlers.Clear();

                if (MethodRets(method, "Boolean"))
                    insts.Add(OpCodes.Ldc_I4_1.ToInstruction());

                insts.Add(OpCodes.Ret.ToInstruction());
            }
            Console.WriteLine();

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

    static bool MethodRets(MethodDef method, string retType = null)
    {
        if (!method.HasReturnType)
            return string.IsNullOrEmpty(retType) || retType == "Void";

        return $"System.{retType}" == method.ReturnType.FullName;
    }

    static void Save(ModuleDefMD moduleDef, string path)
    {
        Console.WriteLine("开始保存修补过的文件中...");

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

        Console.WriteLine("保存修补过的文件成功");
    }
}
