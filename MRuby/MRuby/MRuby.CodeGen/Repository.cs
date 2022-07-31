using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MRuby.CodeGen
{
    public class Registry
    {
        public ClassDesc RootNamespace = new ClassDesc(null, "", 0);

        Dictionary<string, ClassDesc> classes = new Dictionary<string, ClassDesc>();

        public ClassDesc FindOrCreateClassDesc(string fullname, Type t, int pop)
        {
            var cur = RootNamespace;
            var nameList = t.ToString().Split(new char[] { '.', '+' });
            foreach (var name in nameList)
            {
                if (cur.Children.TryGetValue(name, out var found))
                {
                    cur = found;
                }
                else
                {
                    cur = new ClassDesc(cur, name, pop);
                    classes.Add(cur.FullName, cur);
                }
            }

            // Set Type if not set.
            if (t != null)
            {
                Debug.Assert(cur.Type == null || cur.Type == t);
                if (cur.Type != t)
                {
                    classes.Remove(cur.FullName);
                    cur.SetType(t);
                    classes.Add(cur.FullName, cur);

                    if (t.BaseType != null)
                    {
                        FindByType(t.BaseType, cur);
                    }
                }
            }

            if (cur.PopCountFromExport > pop)
            {
                cur.PopCountFromExport = pop;
            }

            return cur;
        }

        public ClassDesc FindByType(Type t, int pop)
        {
            if (classes.TryGetValue(t.ToString(), out var found))
            {
                if (found.PopCountFromExport > pop)
                {
                    found.PopCountFromExport = pop;
                }
                return found;
            }
            else
            {
                return FindOrCreateClassDesc(t.ToString(), t, pop);
            }
        }

        public ClassDesc FindByType(Type t, ClassDesc from)
        {
            return FindByType(t, from.PopCountFromExport + 1);
        }

        public IEnumerable<ClassDesc> AllDescs()
        {
            return classes.Values.ToArray();
        }

    }

    public class ClassDesc
    {
        public ClassDesc Parent { get; private set; }
        public readonly string Name;
        public Type Type { get; private set; }
        public readonly Dictionary<string, ClassDesc> Children = new Dictionary<string, ClassDesc>();

        /// <summary>
        /// C#のなかでの名前
        /// </summary>
        public string FullName { get; private set; }

        public bool Exported;

        /// <summary>
        /// Export指定されたクラスからのポップカウント
        /// (エキスポートされたクラス自体は、0)
        /// </summary>
        public int PopCountFromExport = -1;

        Dictionary<string, MethodDesc> methodDescs = new Dictionary<string, MethodDesc>();
        public readonly IReadOnlyDictionary<string, MethodDesc> MethodDescs;

        List<ConstructorInfo> constructors = new List<ConstructorInfo>();
        public readonly IReadOnlyList<ConstructorInfo> Constructors;

        Dictionary<string, FieldDesc> fields = new Dictionary<string, FieldDesc>();
        public readonly IReadOnlyDictionary<string, FieldDesc> Fields;

        public ClassDesc(ClassDesc parent, string name, int pop)
        {
            Parent = parent;
            Name = name;
            PopCountFromExport = pop;

            if (parent == null || parent.IsRoot)
            {
                FullName = name;
            }
            else
            {
                FullName = parent.FullName + "." + name;
            }

            Parent?.Children.Add(name, this);

            MethodDescs = methodDescs;
            Constructors = constructors;
            Fields = fields;
        }

        public ClassDesc(ClassDesc parent, Type type, int pop) : this(parent, type.Name, pop)
        {
            SetType(type);
        }

        public void SetType(Type t)
        {
            Type = t;
            FullName = t.ToString();
        }

        public bool IsRoot => (Parent == null);
        public bool IsNamespace => (Type == null);
        public Type BaseType => IsNamespace ? null : (Type?.BaseType ?? typeof(System.Object));

        public string CodeName => Naming.CodeName(FullName);
        public string RubyFullName => IsRoot ? "Object" : Naming.RubyName(FullName);
        public string BinderClassName => ExportName;
        public string ExportName
        {
            get
            {

                if (Type.IsGenericType)
                {
                    return "MRuby_" + FullName.Replace(".", "_").Replace("+", "_");
                }
                else
                {
                    return "MRuby_" + FullName.Replace(".", "_").Replace("+", "_"); // TODO
                }
            }
        }


        public MethodDesc AddMethod(MethodInfo m)
        {
            if (!methodDescs.TryGetValue(m.Name, out var found))
            {
                found = new MethodDesc(this, m.Name);
                methodDescs.Add(m.Name, found);
            }
            found.AddMethodInfo(m);
            return found;
        }

        public void AddConstructor(ConstructorInfo c)
        {
            constructors.Add(c);
        }

        public void AddField(FieldInfo f)
        {
            fields.Add(f.Name, new FieldDesc(f));
        }

        public void AddProperty(PropertyInfo p)
        {
            fields.Add(p.Name, new FieldDesc(p));
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
            Debug.Assert(m.Name == Name);
            methods.Add(m);
        }

        public bool IsStatic => methods.All(m => m.IsStatic);

        public (int, int) ParameterNum()
        {
            var min = methods.Min(m => requireParam(m));
            var max = methods.Max(m => m.GetParameters().Length);
            return (min, max);
        }

        int requireParam(MethodInfo m)
        {
            return m.GetParameters().TakeWhile(p => !p.HasDefaultValue).Count();
        }

        public bool IsGeneric => methods.Any(m => m.IsGenericMethod);

        public bool IsOverloaded => (methods.Count > 1);


        public string RubyName => Naming.ToSnakeCase(Name);

    }

    public class FieldDesc
    {
        public readonly string Name;
        public readonly FieldInfo Field;
        public readonly PropertyInfo Property;
        public readonly MemberInfo MemberInfo;

        public FieldDesc(FieldInfo f)
        {
            Name = f.Name;
            Field = f;
            MemberInfo = f;
        }

        public FieldDesc(PropertyInfo p)
        {
            Name = p.Name;
            Property = p;
            MemberInfo = p;
        }

        public bool IsProperty => (Property != null);

        public Type Type => IsProperty ? Property.PropertyType : Field.FieldType;
        public bool CanRead => IsProperty ? Property.CanRead : Field.IsPublic;
        public bool CanWrite => IsProperty ? Property.CanWrite : !(Field.IsLiteral || Field.IsInitOnly);
        public bool IsStatic => IsProperty ? false : Field.IsStatic;

        public string RubyName => Naming.ToSnakeCase(Name);

        public string GetterName => "get_" + Name;
        public string SetterName => "set_" + Name;

    }

}
