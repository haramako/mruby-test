using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace MRuby.CodeGen
{
    public class RegistryPrinter
    {
        int indent;
        bool verbose;

        public RegistryPrinter(bool _verbose = false)
        {
            verbose = _verbose;
        }

        void write(string msg)
        {
            Console.Write(new string(' ', indent * 2));
            Console.WriteLine(msg);
        }

        public void PrintRegistry(Registry reg)
        {
            foreach (var cls in reg.AllNamespaces(reg.RootNamespace))
            {
                write(new string('=', 40));
                PrintClassDesc(cls);
            }
        }

        public void PrintClassDesc(ClassDesc cls)
        {
            if (cls.IsNamespace)
            {
                write($"namespace {cls.FullName} => {cls.RubyFullName}");
            }
            else
            {
                write($"class {cls.FullName} => {cls.RubyFullName}");
            }

            indent++;
            foreach (var m in cls.MethodDescs.Values)
            {
                PrintMethodDesc(m);
            }
            foreach (var f in cls.Fields.Values)
            {
                PrintField(f);
            }
            indent--;
        }

        public void PrintMethodDesc(MethodDesc m)
        {
            var (min, max) = m.ParameterNum();
            var isStatic = m.IsStatic ? "s" : " ";
            write($"{isStatic} {m.Name} => {m.RubyName} ({m.Methods.Count}) {min}..{max}");

            if (verbose)
            {
                indent++;
                foreach (var method in m.Methods)
                {
                    write($"  {method} {method.Attributes}");
                }
                indent--;
            }
        }

        public void PrintField(FieldDesc f)
        {
            var kind = f.IsProperty ? "p" : "f";
            var canRead = f.CanRead ? "r" : "-";
            var canWrite = f.CanWrite ? "w" : "-";
            write($"{kind} {f.Name} {canRead}{canWrite}");
        }
    }

    public class CodeGenUtil
    {
        public CodeGenUtil()
        {

        }

        public void RegisterClass(Registry reg, Type t)
        {
            var curNs = reg.RootNamespace;
            var nameList = t.FullName.Split(new char[] { '.', '+' }).SkipLast(1);
            foreach (var name in nameList)
            {
                if (curNs.Children.TryGetValue(name, out var found))
                {
                    curNs = found;
                }
                else
                {
                    curNs = ClassDesc.CreateOrGet(curNs, name, null);
                }
            }

            var cls = ClassDesc.CreateOrGet(curNs, t.Name, t);
            if (!cls.IsNamespace)
            {
                RegisterClass_(reg, cls);
            }
        }

        public void RegisterClass_(Registry reg, ClassDesc cls)
        {
            var t = cls.Type;
            if (!t.IsGenericTypeDefinition && (!TypeCond.IsObsolete(t)
                && t != typeof(UnityEngine.YieldInstruction) && t != typeof(UnityEngine.Coroutine))
                || (t.BaseType != null && t.BaseType == typeof(System.MulticastDelegate)))
            {
                if (t.IsNested
                    && ((!t.DeclaringType.IsNested && t.DeclaringType.IsPublic == false)
                    || (t.DeclaringType.IsNested && t.DeclaringType.IsNestedPublic == false)))
                {
                    return;
                }

                if (t.IsEnum)
                {
                    // TODO
                }
                else if (t.BaseType == typeof(System.MulticastDelegate))
                {
                    if (t.ContainsGenericParameters)
                    {
                        return;
                    }

                    // TODO

                    return;
                }
                else
                {
                    // Normal methods
                    var constructors = GetValidConstructor(t);
                    foreach (var c in constructors)
                    {
                        cls.AddConstructor(c);
                    }

                    var methods = t.GetMethods();
                    foreach (var m in methods)
                    {
                        if ((m.Attributes & MethodAttributes.SpecialName) != 0) {
                            continue;
                        }
                        cls.AddMethod(m);
                    }

                    var fields = t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var f in fields)
                    {
                        cls.AddField(f);
                    }

                    var properties = t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var p in properties)
                    {
                        cls.AddProperty(p);
                    }
                }

            }
        }

        ConstructorInfo[] GetValidConstructor(Type t)
        {
            List<ConstructorInfo> ret = new List<ConstructorInfo>();
            if (t.GetConstructor(Type.EmptyTypes) == null && t.IsAbstract && t.IsSealed)
                return ret.ToArray();
            if (t.IsAbstract)
                return ret.ToArray();
            if (t.BaseType != null && t.BaseType.Name == "MonoBehaviour")
                return ret.ToArray();

            ConstructorInfo[] cons = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            foreach (ConstructorInfo ci in cons)
            {
                if (!TypeCond.IsObsolete(ci) && !TypeCond.DontExport(ci) && !TypeCond.ContainUnsafe(ci))
                    ret.Add(ci);
            }
            return ret.ToArray();
        }

        private void RegisterClassFunctions(Type t, bool writeStatic)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.DeclaredOnly;
            if (writeStatic)
                bf |= BindingFlags.Static;
            else
                bf |= BindingFlags.Instance;

            MethodInfo[] members = t.GetMethods(bf);
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodInfo mi in members)
            {
                methods.Add(TypeCond.TryFixGenericMethod(mi));
            }

            if (!writeStatic && this.includeExtension)
            {
#if false
                if (extensionMethods.ContainsKey(t))
                {
                    methods.AddRange(extensionMethods[t]);
                }
#endif
            }
            foreach (MethodInfo mi in methods)
            {
                // 一時的に override を無効化
                MethodBase[] cons = GetMethods(t, mi.Name, bf);
                if (cons.Length > 1)
                {
                    continue;
                }

                bool instanceFunc;
                if (writeStatic && TypeCond.IsPInvoke(mi, out instanceFunc))
                {
                    // TODO
                    // directfunc.Add(t.FullName + "." + mi.Name, instanceFunc);
                    continue;
                }

                string fn = writeStatic ? staticName(mi.Name) : mi.Name;
                if (mi.MemberType == MemberTypes.Method
                    && !TypeCond.IsObsolete(mi)
                    && !TypeCond.DontExport(mi)
                    /* && !funcname.Contains(fn) */
                    && TypeCond.IsUsefullMethod(mi)
                    && !TypeCond.MemberInFilter(t, mi)
                    && !TypeCond.ContainUnsafe(mi))
                {
                    // TODO
                }
            }
        }

        bool includeExtension = true;

        MethodBase[] GetMethods(Type t, string name, BindingFlags bf)
        {
            List<MethodBase> methods = new List<MethodBase>();

            if (this.includeExtension && ((bf & BindingFlags.Instance) == BindingFlags.Instance))
            {
#if false
                if (extensionMethods.ContainsKey(t))
                {
                    foreach (MethodInfo m in extensionMethods[t])
                    {
                        if (m.Name == name
                           && !TypeCond.IsObsolete(m)
                           && !TypeCond.DontExport(m)
                           && TypeCond.IsUsefullMethod(m))
                        {
                            methods.Add(m);
                        }
                    }
                }
#endif
            }

            MemberInfo[] cons = t.GetMember(name, bf);
            foreach (MemberInfo _m in cons)
            {
                MemberInfo m = _m;
                if (m.MemberType == MemberTypes.Method) m = TypeCond.TryFixGenericMethod((MethodInfo)m);
                if (m.MemberType == MemberTypes.Method
                    && !TypeCond.IsObsolete(m)
                    && !TypeCond.DontExport(m)
                    && TypeCond.IsUsefullMethod((MethodInfo)m))
                    methods.Add((MethodBase)m);
            }
            methods.Sort((a, b) =>
            {
                return a.GetParameters().Length - b.GetParameters().Length;
            });
            return methods.ToArray();
        }

        static string staticName(string name)
        {
            if (name.StartsWith("op_"))
                return name;
            return name;
            //return name + "_s";
        }

        public static void GenerateBind(Registry reg, string path)
        {
            var nsGen = new NamespaceGen(reg, path);
            nsGen.Generate();
        }

        public static List<Type> GetMRubyClasses(string[] asemblyNames)
        {
            List<Type> exports = new List<Type>();

            exports.Add(typeof(System.Object));

            foreach (string asemblyName in asemblyNames)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.Load(asemblyName);
                }
                catch (Exception)
                {
                    continue;
                }

                Type[] types = assembly.GetExportedTypes();

                foreach (Type t in types)
                {
                    var attr = (CustomMRubyClassAttribute)Attribute.GetCustomAttribute(t, typeof(CustomMRubyClassAttribute));
                    if (attr != null)
                    {
                        exports.Add(t);
                    }
                }
            }
            return exports;
        }


        void FilterSpecMethods(out Dictionary<Type, List<MethodInfo>> dic, out Dictionary<Type, Type> overloadedClass)
        {
            dic = new Dictionary<Type, List<MethodInfo>>();
            overloadedClass = new Dictionary<Type, Type>();
            List<string> asems;
            CustomExport.OnGetAssemblyToGenerateExtensionMethod(out asems);

            // Get list from custom export.
            object[] aCustomExport = new object[1];
#if !SLUA_STANDALONE
            LuaCodeGen.InvokeEditorMethod<ICustomExportPost>("OnGetAssemblyToGenerateExtensionMethod", ref aCustomExport);
#endif
            if (null != aCustomExport[0])
            {
                if (null != asems)
                {
                    asems.AddRange((List<string>)aCustomExport[0]);
                }
                else
                {
                    asems = (List<string>)aCustomExport[0];
                }
            }

            foreach (string assstr in asems)
            {
                Assembly assembly = Assembly.Load(assstr);
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (type.IsSealed && !type.IsGenericType && !type.IsNested)
                    {
                        MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
                        foreach (MethodInfo _method in methods)
                        {
                            MethodInfo method = TypeCond.TryFixGenericMethod(_method);
                            if (TypeCond.IsExtensionMethod(method))
                            {
                                Type extendedType = method.GetParameters()[0].ParameterType;
                                if (!dic.ContainsKey(extendedType))
                                {
                                    dic.Add(extendedType, new List<MethodInfo>());
                                }
                                dic[extendedType].Add(method);
                            }
                        }
                    }


                    if (type.IsDefined(typeof(OverloadLuaClassAttribute), false))
                    {
                        OverloadLuaClassAttribute olc = type.GetCustomAttributes(typeof(OverloadLuaClassAttribute), false)[0] as OverloadLuaClassAttribute;
                        if (olc != null)
                        {
                            if (overloadedClass.ContainsKey(olc.targetType))
                                throw new Exception("Can't overload class more than once");
                            overloadedClass.Add(olc.targetType, type);
                        }
                    }
                }
            }
        }

    }
}
