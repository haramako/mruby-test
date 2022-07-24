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
        public bool Ordered;

        private NamespaceInfo(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public bool IsRoot => (Parent == null);
        public bool IsNamespace => (Type == null);
        public Type BaseType => IsNamespace ? null : (Type?.BaseType ?? typeof(System.Object));

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

        public string RubyFullName => IsRoot ? "Object" : FullName.Replace(".", "::");

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

}
