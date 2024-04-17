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
        Console.Title = "Eazfuscator.NET 7天试用移除工具";

        if (args.Length == 0)
        {
            Console.WriteLine("未发现运行参数！按回车键退出");
            Console.ReadLine();
            return;
        }

        var path = args[0];
        Console.WriteLine($"传入文件路径 {path}");

        if (!path.EndsWith("dll"))
        {
            Console.WriteLine("请输入dll文件！按回车键退出");
            Console.ReadLine();
            return;
        }

        if (!File.Exists(path))
        {
            Console.WriteLine("输入dll文件不存在！按回车键退出");
            Console.ReadLine();
            return;
        }

        Patch(path);

        Console.WriteLine("\n============================");
        Console.WriteLine("检查修改结果");
        Patch(path, false);

        Console.WriteLine("程序执行完毕！按回车键退出");
        Console.ReadLine();
    }

    static void Patch(string path, bool isSave = true)
    {
        Console.WriteLine("开始查找类...");

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
        Console.WriteLine("开始保存修补过的文件...");

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
