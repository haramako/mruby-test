using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MRuby.CodeGen
{
    class NamespaceGen
    {
        CodeWriter w;
        Registry reg;

        public NamespaceGen(Registry _reg, string path)
        {
            reg = _reg;
            w = new CodeWriter(Path.Combine(path, "MRuby__Namespaces.cs"));
        }

        public void Generate()
        {
            w.Write("using MRuby;");
            w.Write("[MRuby.LuaBinder({0})]", 0);
            w.Write("public class _Binder {");

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
                    string baseType;
                    if(cls.Type == typeof(Object))
                    {
                        baseType = "null";
                    }
                    else
                    {
                        baseType = "\"" + reg.FindByType(cls.BaseType, cls).RubyFullName + "\"";
                    }
                    w.Write("new RuntimeClassDesc( \"{0}\", {1}.RegisterMembers, {2}),", cls.RubyFullName, cls.BinderClassName, baseType);
                }
            }

            w.Write("};");
            w.Write("}");

            w.Dispose();
        }

    }
}
