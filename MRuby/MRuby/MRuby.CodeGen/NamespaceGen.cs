using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MRuby.CodeGen
{
    public class NamespaceInfo
    {
        public NamespaceInfo Parent { get; private set; }
        public readonly string Name;
        public Type Type { get; private set; }
        public readonly Dictionary<string, NamespaceInfo> Children = new Dictionary<string, NamespaceInfo>();

        private NamespaceInfo(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public bool IsRoot => (Parent == null);
        public bool IsNamespace => (Type == null);

        public string FullName
        {
            get
            {
                if (IsRoot)
                {
                    return "";
                }
                else if (Parent.IsRoot)
                {
                    return Name;
                }
                else
                {
                    return Parent.FullName + "." + Name;
                }
            }
        }

        public string RubyFullName => FullName.Replace(".", "_");

        /// <summary>
        /// Name of c# variable name.
        /// </summary>
        public string VarFullName => FullName.Replace(".", "_");


        public static NamespaceInfo CreateRoot() => new NamespaceInfo("", null);
        public static NamespaceInfo CreateOrGet(NamespaceInfo parent, string name, Type type)
        {
            NamespaceInfo ns;
            if (parent == null)
            {
                ns = new NamespaceInfo(name, type);
            }
            else
            {
                if (parent.Children.TryGetValue(name, out var found))
                {
                    ns = found;
                    ns.Type = type;
                }
                else
                {
                    ns = new NamespaceInfo(name, type);
                    ns.Parent = parent;
                    parent.Children.Add(name, ns);
                }
            }
            return ns;
        }

    }

    public class Registry
    {
        public NamespaceInfo RootNamespace = NamespaceInfo.CreateRoot();

        public NamespaceInfo FindByType(Type t)
        {
            return AllNamespaces(RootNamespace).Where(ns => ns.Type == t).FirstOrDefault();
        }

        public IEnumerable<NamespaceInfo> AllNamespaces(NamespaceInfo cur)
        {
            yield return cur;
            foreach (var child in cur.Children.Values)
            {
                foreach (var childNs in AllNamespaces(child))
                {
                    yield return childNs;
                }
            }
        }

    }

    class CodeWriter : IDisposable
    {
        public static EOL eol = MRubySetting.Instance.eol;

        int indent = 0;
        StreamWriter w;

        public CodeWriter(string path)
        {
            w = new StreamWriter(Path.Combine(path, "MRuby__Namespaces.cs"), false, Encoding.UTF8);
        }



        string NewLine
        {
            get
            {
                switch (eol)
                {
                    case EOL.Native:
                        return System.Environment.NewLine;
                    case EOL.CRLF:
                        return "\r\n";
                    case EOL.CR:
                        return "\r";
                    case EOL.LF:
                        return "\n";
                    default:
                        return "";
                }
            }
        }

        public void Dispose()
        {
            if (w != null)
            {
                w.Close();
                w = null;
            }
        }

        public void Write(string fmt, params object[] args)
        {
            fmt = System.Text.RegularExpressions.Regex.Replace(fmt, @"\r\n?|\n|\r", NewLine);

            if (fmt.StartsWith("}")) indent--;

            for (int n = 0; n < indent; n++)
            {
                w.Write("\t");
            }

            if (args.Length == 0)
            {
                w.WriteLine(fmt);
            }
            else
            {
                string line = string.Format(fmt, args);
                w.WriteLine(line);
            }

            if (fmt.EndsWith("{")) indent++;
        }
    }

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
            w.Write("RClass baseClass;");
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
                w.Write("var _mod_{0} = DLL.mrb_class_get(mrb, \"Object\");", ns.VarFullName);
            }
            else if (ns.IsNamespace)
            {
                w.Write("var _mod_{0} = DLL.mrb_define_module_under(mrb, _mod_{1}, \"{2}\");", ns.VarFullName, ns.Parent.VarFullName, ns.Name);
            }
            else
            {
                var baseType = ns.Type.BaseType;
                var baseTypeNs = reg.FindByType(baseType);
                if (baseTypeNs == null)
                {
                    w.Write("baseClass = Converter.GetClass(mrb, \"{0}\");", "System.Object");
                }
                else
                {
                    w.Write("baseClass = Converter.GetClass(mrb, \"{0}\");", baseTypeNs.FullName);
                }
                w.Write("var _mod_{0} = DLL.mrb_define_class_under(mrb, _mod_{1}, \"{2}\", baseClass);", ns.VarFullName, ns.Parent.VarFullName, ns.Name);
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
