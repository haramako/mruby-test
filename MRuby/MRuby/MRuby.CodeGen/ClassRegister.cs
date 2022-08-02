using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace MRuby.CodeGen
{
    public class ClassRegister
    {
        public ClassRegister()
        {

        }

        public void RegisterClass(Registry reg, Type t)
        {
            var cls = reg.FindByType(t, 0);

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
                    // Normal methods
                    var constructors = GetValidConstructor(t);
                    foreach (var c in constructors)
                    {
                        cls.AddConstructor(c);
                    }

                    var methods = t.GetMethods();
                    foreach (var m in methods)
                    {
                        var noinstance = TypeUtil.IsStaticClass(t) && !m.IsStatic;
                        if (TypeUtil.IsPropertyAccessor(m) || noinstance)
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
                if (!TypeUtil.IsObsolete(ci) && !TypeUtil.DontExport(ci) && !TypeUtil.ContainUnsafe(ci))
                    ret.Add(ci);
            }
            return ret.ToArray();
        }

        public static List<Type> GetMRubyClasses(Assembly assembly)
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

        public static List<Type> GetMRubyClasses(string[] asemblyNames)
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
