using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

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
                    cur = new ClassDesc(cur, name, Math.Max(0, pop));
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

            if (pop >= 0 && cur.PopCountFromExport > pop)
            {
                cur.PopCountFromExport = pop;
            }

            return cur;
        }

        public ClassDesc FindByType(Type t, int pop)
        {
            if (classes.TryGetValue(t.ToString(), out var found))
            {
                if (pop >= 0 && found.PopCountFromExport > pop)
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

        public bool Registered;

        public bool Exported;

        /// <summary>
        /// Export指定されたクラスからのポップカウント
        /// (エキスポートされたクラス自体は、0)
        /// </summary>
        public int PopCountFromExport = -1;

        Dictionary<string, MethodDesc> methodDescs = new Dictionary<string, MethodDesc>();
        public readonly IReadOnlyDictionary<string, MethodDesc> MethodDescs;

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


        public MethodDesc AddMethod(MethodEntry m, bool isConstructor = false)
        {
            var name = (isConstructor ? "__initialize__" : m.Name);
            if (!methodDescs.TryGetValue(name, out var found))
            {
                found = new MethodDesc(this, name, isConstructor);
                methodDescs.Add(name, found);
            }
            found.AddMethodEntry(m);
            return found;
        }

        public void AddConstructor(ConstructorInfo c)
        {
            AddMethod(new MethodEntry(c), isConstructor: true);
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

    /// <summary>
    /// A information of method group.
    /// 
    /// Include overloaded methods.
    /// </summary>
    public class MethodDesc
    {
        List<MethodEntry> methods = new List<MethodEntry>();
        public readonly IReadOnlyList<MethodEntry> Methods;

        public readonly string Name;
        public readonly bool IsConstructor;

        public MethodDesc(ClassDesc owner, string name, bool isConstructor = false)
        {
            Methods = methods;
            Name = name;
            IsConstructor = isConstructor;
        }

        public void AddMethodEntry(MethodEntry m)
        {
            Debug.Assert(IsConstructor || m.Info.Name == Name);
            methods.Add(m);
        }

        public bool IsStatic => methods.All(m => m.IsStatic);

        /// <summary>
        /// 
        /// </summary>
        /// <returns>returns (min parameter num, max parameter num, has variable parameter)</returns>
        public (int, int, bool) ParameterNum()
        {
            var min = methods.Min(m => m.RequiredParamNum);
            var max = methods.Max(m => m.ParamNum);
            var hasParamArray = methods.Any(m => m.HasParamArray);
            return (min, max, hasParamArray);
        }

        public bool IsGeneric => methods.Any(m => m.IsGeneric);

        public bool IsOverloaded => (methods.Count > 1);

        public string RubyName => IsConstructor ? "initialize" : Naming.ToSnakeCase(Name);
    }

    /// <summary>
    /// A info of one method.
    /// 
    /// It's a MethodBase (MethodInfo and ConstructorInfo) with additional information.
    /// </summary>
    public class MethodEntry
    {
        public readonly MethodBase Info;
        public readonly ParameterInfo[] Parameters;
        public readonly string Name;

        /// <summary>
        /// Is extension-method.
        /// </summary>
        public readonly bool IsExtension;


        /// <summary>
        /// Number of this param. (1 if extension method else 0)
        /// </summary>
        public readonly int ThisParamNum;

        /// <summary>
        /// Has variable length parameters, like 'params object[] args'.
        /// </summary>
        public readonly bool HasParamArray;

        /// <summary>
        /// Is generic method.
        /// 
        /// Generic class's normal method is false.
        /// </summary>
        public readonly bool IsGeneric;

        /// <summary>
        /// Is static method in ruby. (Extension method is not static)
        /// </summary>
        public readonly bool IsStatic;

        /// <summary>
        /// Min parameter number in ruby.
        /// </summary>
        public readonly int RequiredParamNum;

        /// <summary>
        /// Maximum parameter number in ruby.
        /// </summary>
        public readonly int ParamNum;

        public readonly Type ReturnType;

        public MethodEntry(MethodBase info, bool isExtension = false, bool isConstructor = false)
        {
            Info = info;
            Parameters = info.GetParameters();
            Name = info.Name;
            IsExtension = isExtension;
            HasParamArray = Parameters.LastOrDefault()?.IsDefined(typeof(ParamArrayAttribute)) ?? false;
            IsGeneric = info.IsGenericMethod;

            ThisParamNum = (isExtension ? 1 : 0);
            IsStatic = info.IsStatic && !IsExtension;
            RequiredParamNum = Parameters.TakeWhile(p => !p.HasDefaultValue).Count() - ThisParamNum;
            ParamNum = Parameters.Length - ThisParamNum;

            ReturnType = (Info as MethodInfo)?.ReturnType;
        }
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
