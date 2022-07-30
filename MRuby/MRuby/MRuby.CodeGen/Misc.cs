﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace MRuby.CodeGen
{
    public class ClassDesc
    {
        public ClassDesc Parent { get; private set; }
        public readonly string Name;
        public Type Type { get; private set; }
        public readonly Dictionary<string, ClassDesc> Children = new Dictionary<string, ClassDesc>();
        public bool Ordered;

        Dictionary<string, MethodDesc> methodDescs = new Dictionary<string, MethodDesc>();

        List<ConstructorInfo> constructors = new List<ConstructorInfo>();
        public readonly IReadOnlyList<ConstructorInfo> Constructors;

        private ClassDesc(string name, Type type)
        {
            Name = name;
            Type = type;
            Constructors = constructors;
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
                else if (!IsNamespace)
                {
                    return Type.FullName;
                }
                else
                {

                    return Parent.FullName + "." + Name;
                }
            }
        }

        public string RubyFullName => IsRoot ? "Object" : FullName.Replace(".", "::").Replace("+", "::");
        public string BinderClassName => "MRuby_" + FullName.Replace('.', '_').Replace('+', '_');


        public MethodDesc AddMethod(MethodInfo m)
        {
            if( !methodDescs.TryGetValue( m.Name, out var found))
            {
                found = new MethodDesc(this, m.Name);
                methodDescs.Add(m.Name, found);
            }
            return found;
        }

        public void AddConstructor(ConstructorInfo c)
        {
            constructors.Add(c);
        }

        public static ClassDesc CreateRoot() => new ClassDesc("", null);
        public static ClassDesc CreateOrGet(ClassDesc parent, string name, Type type)
        {
            ClassDesc ns;
            if (parent == null)
            {
                ns = new ClassDesc(name, type);
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
                    ns = new ClassDesc(name, type);
                    ns.Parent = parent;
                    parent.Children.Add(name, ns);
                }
            }
            return ns;
        }

    }

    public class MethodDesc
    {
        List<MethodInfo> methods = new List<MethodInfo>();
        public readonly IReadOnlyList<MethodInfo> Methods;

        public readonly string Name;

        public MethodDesc(ClassDesc owner, string name)
        {
            Methods = methods;
            Name = name;
        }

        public void AddMethodInfo(MethodInfo m)
        {
            Debug.Assert(m.Name != Name);
            methods.Add(m);
        }

    }

    public class Registry
    {
        public ClassDesc RootNamespace = ClassDesc.CreateRoot();

        public ClassDesc FindByType(Type t)
        {
            return AllNamespaces(RootNamespace).Where(ns => ns.Type == t).FirstOrDefault();
        }

        public IEnumerable<ClassDesc> AllNamespaces(ClassDesc cur)
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
