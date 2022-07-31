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
            var list = new List<ClassDesc>();
            makeGenerateOrder(list, reg.RootNamespace);

            foreach (var ns in list)
            {
                Logger.Log(ns.FullName);
            }

            w.Write("using System;");
            w.Write("using System.Collections.Generic;");
            w.Write("namespace MRuby {");
            w.Write("[LuaBinder({0})]", 0);
            w.Write("public class _Binder {");

            w.Write("public static void Bind(mrb_state mrb){");
            w.Write("registerNamespaces(mrb);");
            w.Write("registerClasses(mrb);");
            w.Write("}");
            w.Write("");

            generateNamespaces(list);
            generateClassBinders(list);

            w.Write("}");
            w.Write("}");

            w.Dispose();
        }

        void makeGenerateOrder(List<ClassDesc> list, ClassDesc ns)
        {
            if (ns.Ordered)
            {
                return;
            }

            if (ns.Type != null && ns.Type.BaseType != null)
            {
                var baseType = reg.FindByType(ns.Type.BaseType, ns);
                makeGenerateOrder(list, baseType);
            }

            list.Add(ns);
            ns.Ordered = true;

            foreach (var child in ns.Children.Values)
            {
                makeGenerateOrder(list, child);
            }

        }

        void generateNamespaces(List<ClassDesc> list)
        {
            w.Write("static void defineModule(mrb_state mrb, string name, string ns){");
            w.Write("DLL.mrb_define_module_under(mrb, Converter.GetClass(mrb, ns), name);");
            w.Write("}");
            w.Write("static void defineClass(mrb_state mrb, string name, string ns, string baseClass){");
            w.Write("DLL.mrb_define_class_under(mrb, Converter.GetClass(mrb, ns), name, Converter.GetClass(mrb, baseClass));");
            w.Write("}");
            w.Write("");
            w.Write("static void registerNamespaces(mrb_state mrb) {");
            foreach (var ns in list)
            {
                generateNamespace(ns);
            }
            w.Write("}");
        }

        void generateNamespace(ClassDesc ns)
        {
            if (ns.IsRoot)
            {
                // DO NOTHING
            }
            else if (ns.IsNamespace)
            {
                w.Write("defineModule(mrb, \"{0}\", \"{1}\");", ns.Name, ns.Parent.RubyFullName);
            }
            else
            {
                var baseType = ns.Type.BaseType ?? typeof(System.Object);
                var baseTypeNs = reg.FindByType(baseType, 0);
                w.Write("defineClass(mrb, \"{0}\", \"{1}\", \"{2}\");", ns.Name, ns.Parent.RubyFullName, baseTypeNs.RubyFullName);
            }
        }

        void generateClassBinders(List<ClassDesc> list)
        {
            w.Write("static void registerClasses(mrb_state mrb) {");
            foreach (var ns in list)
            {
                if (!ns.IsNamespace)
                {
                    w.Write("{0}.RegisterMembers(mrb);", ns.BinderClassName);
                }
            }
            w.Write("}");

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
