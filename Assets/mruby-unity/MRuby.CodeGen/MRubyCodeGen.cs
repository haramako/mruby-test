using System;
using System.IO;

namespace MRuby.CodeGen
{
    public class MRubyCodeGen
    {
        public class Option
        {
            public string OutputDir = "MRubyBinding";
            public bool ExportAllAssembly = true;
            public string[] ExportAssembliyNames = new string[0];
            public Type[] ExportTypes = new Type[] { typeof(System.Object) };
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

}
