using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MRuby.CodeGen
{
    public class BindingGenerator
    {
        CodeWriter w;
        Registry reg;
        string binderClassName;

        public BindingGenerator(Registry _reg, string path, string _binderClassName)
        {
            reg = _reg;
            w = new CodeWriter(path);
            binderClassName = _binderClassName;
        }

        public void Generate()
        {
            w.Write("using MRuby;");
            w.Write("[MRuby.MRubyBinder]");
            w.Write("public class {0} {{", binderClassName);

            w.Write("public static RuntimeClassDesc[] BindData = new[]");
            w.Write("{");

            foreach (var cls in reg.AllDescs())
            {
                if (cls.IsNamespace)
                {
                    w.Write("new RuntimeClassDesc( \"{0}\", null, null),", cls.RubyFullName);
                }
                else
                {
                    if (cls.Exported)
                    {
                        string baseType;
                        if (cls.Type == typeof(Object))
                        {
                            baseType = "null";
                        }
                        else if (!reg.FindByType(cls.BaseType, cls).Exported)
                        {
                            baseType = "\"" + "System::Object" + "\"";
                        }
                        else
                        {
                            baseType = "\"" + reg.FindByType(cls.BaseType, cls).RubyFullName + "\"";
                        }
                        w.Write("new RuntimeClassDesc( \"{0}\", {1}.Register, {2}),", cls.RubyFullName, cls.BinderClassName, baseType);
                    }
                }
            }

            w.Write("};");
            w.Write("}");

            w.Dispose();
        }

    }
}
