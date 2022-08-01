﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MRuby;
using MRuby.CodeGen;

class Program
{
    static void Main(string[] args)
    {
        // For testing.
        if (false)
        {
            test();
            Environment.Exit(0);
        }


        var reg = new Registry();

        var gen = new CodeGenUtil();
        var mrubyTypes = CodeGenUtil.GetMRubyClasses(new string[] { "MRubyLib" });
        mrubyTypes.Add(typeof(System.Object));
        //mrubyTypes.Add(typeof(System.Array));

        foreach (var t in mrubyTypes)
        {
            //Console.WriteLine(t);
            gen.RegisterClass(reg, t);
        }

        new RegistryPrinter(2).PrintRegistry(reg);

        var path = "../CodeGenTest/AutoGenerated/";

        foreach (var cls in reg.AllDescs())
        {
            if (!cls.IsNamespace && !cls.Type.IsGenericType)
            {
                CodeGenerator cg = new CodeGenerator(reg, cls, path);
                cg.givenNamespace = "";
                cg.Generate();
            }
        }

        new NamespaceGen(reg, Path.Combine(path, "_Binder.cs"), "_Binder").Generate();
    }

    static void test()
    {
        Console.WriteLine(typeof(Dictionary<int, int>.KeyCollection)); // System.Collections.Generic.Dictionary`2+KeyCollection[System.Int32,System.Int32]
        Console.WriteLine(typeof(List<int>)); // System.Collections.Generic.List`1[System.Int32]
        Console.WriteLine(typeof(ClassInClass.ClassInClassChild)); // ClassInClass+ClassInClassChild
        Console.WriteLine(typeof(Object).BaseType); // null
        Console.WriteLine(typeof(String).BaseType); // System.Object

        Console.WriteLine();
        foreach (var m in typeof(Object).GetMethods())
        {
            Console.WriteLine($"{m.Name} {m}");
        }

        var ex = new Extended();
        ex.Set(1);
        //ex.Set(1.0);
        ex.ExSet(1);

        {
            var x = typeof(ExtTest);
            foreach (var m in typeof(Extended).GetMethods())
            {
                Console.WriteLine(m.ToString());
            }
            Console.WriteLine();
            foreach (var m in typeof(ExtTest).GetMethods())
            {
                Console.WriteLine($"{m} {TypeCond.IsExtensionMethod(m)}");
            }
        }
    }

}
