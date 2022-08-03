using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MRuby.CodeGen
{
    public class TypeCollector
    {
        public TypeCollector()
        {

        }

        public void RegisterType(Registry reg, IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                RegisterType(reg, t);
            }
        }

        public void RegisterType(Registry reg, Type t, int pop = 0)
        {
            if (t == null) return;

            var cls = reg.FindByType(t, pop);

            if (cls.Registered) return;

            if (!t.IsGenericTypeDefinition && (!TypeUtil.IsObsolete(t)
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
                    // Normal methodsrr
                    var constructors = getValidConstructor(t);
                    foreach (var c in constructors)
                    {
                        cls.AddConstructor(c);
                    }

                    var methods = t.GetMethods();
                    foreach (var m in methods)
                    {
                        var noinstance = TypeUtil.IsStaticClass(t) && !m.IsStatic;
                        if (TypeUtil.IsPropertyAccessor(m) || !TypeUtil.IsUsefullMethod(m) || noinstance)
                        {
                            continue;
                        }

                        if (TypeUtil.IsExtensionMethod(m))
                        {
                            var extensionTargetClass = reg.FindByType(TypeUtil.ExtensionTargetClass(m), cls);
                            extensionTargetClass.AddMethod(new MethodEntry(m, true));
                        }
                        else
                        {
                            cls.AddMethod(new MethodEntry(m, false));
                        }
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

                    cls.Registered = true;
                }

            }
        }

        ConstructorInfo[] getValidConstructor(Type t)
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
                if (!TypeUtil.IsObsolete(ci) && !TypeUtil.DontExport(ci) && !TypeUtil.ContainUnsafe(ci))
                    ret.Add(ci);
            }
            return ret.ToArray();
        }

        public List<Type> CollectFromAllAssembries()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.SelectMany(a => CollectFromAssembly(a)).ToList();
        }

        public List<Type> CollectFromAssembly(Assembly assembly)
        {
            List<Type> exports = new List<Type>();
            Type[] types = assembly.GetExportedTypes();

            foreach (Type t in types)
            {
                var attr = (CustomMRubyClassAttribute)Attribute.GetCustomAttribute(t, typeof(CustomMRubyClassAttribute));
                if (attr != null)
                {
                    exports.Add(t);
                }
            }
            return exports;
        }

        public List<Type> CollectFromAssembly(params string[] asemblyNames)
        {
            List<Type> exports = new List<Type>();

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
    }
}
