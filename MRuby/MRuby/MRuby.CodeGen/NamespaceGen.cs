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
            w = new CodeWriter(path);
        }

        public void Generate()
        {
            w.Write("using System;");
            w.Write("using System.Collections.Generic;");
            w.Write("namespace MRuby {");

            generateNamespaces(reg.RootNamespace);

            w.Write("}");

            w.Dispose();
        }

        void generateNamespaces(NamespaceInfo ns)
        {
            w.Write("[LuaBinder({0})]", 0);
            w.Write("public class _Binder {");
            w.Write("public static void RegisterNamespaces(mrb_state mrb) {");
            generateNamespace(ns);
            w.Write("}");
            w.Write("}");
        }

        string modName(NamespaceInfo ns)
        {
            if (ns.IsRoot)
            {
                return "__Root__";
            }
            else
            {
                return ns.VarFullName;
            }
        }

        void generateNamespace(NamespaceInfo ns)
        {
            if (ns.IsRoot)
            {
                // DO NOTHING
            }
            else if (ns.IsNamespace)
            {
                w.Write("DLL.mrb_define_module_under(mrb, Converter.GetClass(mrb, \"{3}\"), \"{2}\");", ns.VarFullName, ns.Parent.VarFullName, ns.Name, ns.Parent.RubyFullName);
            }
            else
            {
                var baseType = ns.Type.BaseType ?? typeof(System.Object);
                var baseTypeNs = reg.FindByType(baseType);
                w.Write("DLL.mrb_define_class_under(mrb, Converter.GetClass(mrb, \"{1}\"), \"{2}\", Converter.GetClass(mrb, \"{3}\"));", ns.VarFullName, ns.Parent.RubyFullName, ns.Name, baseTypeNs.RubyFullName);
            }
            foreach (var child in ns.Children.Values)
            {
                generateNamespace(child);
            }
        }

#if false
        public void GenerateBind(List<Type> list, int order)
        {
            HashSet<Type> exported = new HashSet<Type>();
            w.Write("#if true");
            w.Write("using System;");
            w.Write("using System.Collections.Generic;");
            w.Write("namespace MRuby {");
            w.Write("[LuaBinder({0})]", order);
            w.Write("public class {0} {{", name);
            w.Write("public static Action<mrb_state>[] GetBindList() {");
            w.Write("Action<mrb_state>[] list= {");
            foreach (Type t in list)
            {
                WriteBindType(t, list, exported);
            }
            w.Write("};");
            w.Write("return list;");
            w.Write("}");
            w.Write("}");
            w.Write("}");
            w.Write("#endif");
        }

        void WriteBindType(Type t, List<Type> exported, HashSet<Type> binded)
        {
            if (t == null || binded.Contains(t) || !exported.Contains(t))
                return;

            WriteBindType(file, t.BaseType, exported, binded);
            w.Write("{0}.reg,", ExportName(t), binded);
            binded.Add(t);
        }
#endif

    }
}
