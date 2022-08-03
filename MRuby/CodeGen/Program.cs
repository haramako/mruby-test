﻿using System;
using System.Collections.Generic;
using System.IO;
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

        if (true)
        {
            simple();
        }
        else
        {
            custom();
        }
    }

    public class MRubyCodeGen
    {
        public class Option
        {
            public string OutputDir = "MRubyBinding";
            public bool ExportAllAssembly = true;
            public string[] ExportAssembliyNames = new string[0];
            public Type[] ExportTypes = new Type[] { typeof(System.Object), typeof(System.Array) };
            public string BinderClassName = "_Binder";
            public string Namespace = null;
        }

        public Registry Registry => reg;

        readonly Registry reg = new Registry();
        readonly Option opt;


        public MRubyCodeGen(Option _opt = null)
        {
            opt = _opt ?? new Option();
        }

        public void Collect()
        {
            var collector = new TypeCollector();
            if (opt.ExportAllAssembly)
            {
                collector.RegisterType(reg, collector.CollectFromAllAssembries());
            }
            else
            {
                collector.RegisterType(reg, collector.CollectFromAssembly(opt.ExportAssembliyNames));
            }
            collector.RegisterType(reg, opt.ExportTypes);
        }

        public void Generate()
        {
            foreach (var cls in reg.AllDescs())
            {
                if (!cls.IsNamespace && !cls.Type.IsGenericType)
                {
                    CodeGenerator cg = new CodeGenerator(reg, cls, opt.OutputDir);
                    cg.givenNamespace = "";
                    cg.Generate();
                }
            }

            new BindingGenerator(reg, Path.Combine(opt.OutputDir, opt.BinderClassName + ".cs"), opt.BinderClassName).Generate();
        }

        public static Registry Run(Option opt = null)
        {
            var gen = new MRubyCodeGen(opt);
            gen.Collect();
            gen.Generate();
            return gen.Registry;
        }
    }

    static void simple()
    {
        var opt = new MRubyCodeGen.Option()
        {
            OutputDir = "../CodeGenTest/AutoGenerated",
        };
        var reg = MRubyCodeGen.Run(opt);
        //new RegistryPrinter(1).PrintRegistry(reg);
    }

    static void custom()
    {
        var reg = new Registry();
        var collector = new TypeCollector();
        var exports = collector.CollectFromAssembly("MRubyLib");
        exports.Add(typeof(System.Object));
        //mrubyTypes.Add(typeof(System.Array));

        collector.RegisterType(reg, exports);

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

        new BindingGenerator(reg, Path.Combine(path, "_Binder.cs"), "_Binder").Generate();
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
                Console.WriteLine($"{m} {TypeUtil.IsExtensionMethod(m)}");
            }
        }
    }

}
