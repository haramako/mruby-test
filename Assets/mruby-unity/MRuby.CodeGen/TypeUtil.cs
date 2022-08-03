using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MRuby.CodeGen
{
    public static class TypeUtil
    {
        public static bool DontExport(MemberInfo mi)
        {
            if (mi is null)
            {
                throw new ArgumentNullException(nameof(mi));
            }

            var methodString = string.Format("{0}.{1}", mi.DeclaringType, mi.Name);
            // directly ignore any components .ctor
            if (mi.DeclaringType.IsSubclassOf(typeof(UnityEngine.Component)))
            {
                if (mi.MemberType == MemberTypes.Constructor)
                {
                    return true;
                }
            }

            if (mi.DeclaringType.IsGenericType && mi.DeclaringType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                if (mi.MemberType == MemberTypes.Constructor)
                {
                    ConstructorInfo constructorInfo = mi as ConstructorInfo;
                    var parameterInfos = constructorInfo.GetParameters();
                    if (parameterInfos.Length > 0)
                    {
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(parameterInfos[0].ParameterType))
                        {
                            return true;
                        }
                    }
                }
                else if (mi.MemberType == MemberTypes.Method)
                {
                    var methodInfo = mi as MethodInfo;
                    if (methodInfo.Name == "TryAdd" || methodInfo.Name == "Remove" && methodInfo.GetParameters().Length == 2)
                    {
                        return true;
                    }
                }
            }

            return mi.IsDefined(typeof(DoNotToMRubyAttribute), false);
        }

        public static bool ContainUnsafe(MethodBase mi)
        {
            foreach (ParameterInfo p in mi.GetParameters())
            {
                if (p.ParameterType.ToString().Contains("*"))
                    return true;
            }
            return false;
        }

        public static bool IsPInvoke(MethodInfo mi, out bool instanceFunc)
        {
            instanceFunc = true;
            return false;
        }

        public static bool IsUsefullMethod(MethodInfo method)
        {
            if (method.Name != "GetType" && method.Name != "GetHashCode" && method.Name != "Equals" &&
                /* method.Name != "ToString" && */ method.Name != "Clone" &&
                method.Name != "GetEnumerator" && method.Name != "CopyTo" &&
                method.Name != "op_Implicit" && method.Name != "op_Explicit" &&
                method.Name != "StartCoroutine_Auto" &&
                method.Name != "GetComponents" &&
                method.Name != "ReferenceEquals" &&
                !method.Name.StartsWith("get_", StringComparison.Ordinal) &&
                !method.Name.StartsWith("set_", StringComparison.Ordinal) &&
                !method.Name.StartsWith("add_", StringComparison.Ordinal) &&
                !IsObsolete(method) && !method.ContainsGenericParameters &&
                method.ToString() != "Int32 Clamp(Int32, Int32, Int32)" &&
                !method.Name.StartsWith("remove_", StringComparison.Ordinal) /*&&
                method.MemberType == MemberTypes.Constructor*/)
            {
                return true;
            }
            return false;
        }

        public static bool IsObsolete(MemberInfo t)
        {
            return t.IsDefined(typeof(ObsoleteAttribute), false);
        }

        public static bool MemberInFilter(Type t, MemberInfo mi)
        {
            // TODO
            return true;
        }

        public static bool IsExtensionMethod(MethodBase method)
        {
            return method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);
        }

        public static Type ExtensionTargetClass(MethodBase method)
        {
            var x = method.GetParameters()[0];
            return method.GetParameters()[0].ParameterType;
        }

        public static bool IsStaticClass(Type t)
        {
            return t.IsAbstract && t.IsSealed;
        }

        // try filling generic parameters
        public static MethodInfo TryFixGenericMethod(MethodInfo method)
        {
            if (!method.ContainsGenericParameters)
            {
                return method;
            }

            try
            {
                Type[] genericTypes = method.GetGenericArguments();
                for (int j = 0; j < genericTypes.Length; j++)
                {
                    Type[] contraints = genericTypes[j].GetGenericParameterConstraints();
                    if (contraints != null && contraints.Length == 1 && contraints[0] != typeof(ValueType))
                        genericTypes[j] = contraints[0];
                    else
                        return method;
                }
                // only fixed here
                return method.MakeGenericMethod(genericTypes);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return method;
        }

        public static bool IsValueType(Type t)
        {
            if (t.IsByRef) t = t.GetElementType();
            return t.BaseType == typeof(ValueType) && !IsBaseType(t);
        }

        public static bool IsBaseType(Type t)
        {
            return t.IsPrimitive;
        }

        public static bool IsPropertyAccessor(MethodInfo m)
        {
            return (m.Attributes & MethodAttributes.SpecialName) != 0;
        }

        public static string SimpleTypeName(Type t)
        {
            string tn = t.Name;
            switch (tn)
            {
                case "Single":
                    return "float";
                case "String":
                    return "string";
                case "Double":
                    return "double";
                case "Boolean":
                    return "bool";
                case "Int32":
                    return "int";
                case "Object":
                    //return reg.FindByType(t).FullName;
                    return "object";
                default:
                    tn = TypeDecl(t);
                    tn = tn.Replace("System.Collections.Generic.", "");
                    tn = tn.Replace("System.Object", "object");
                    return tn;
            }
        }

        static string[] prefix = new string[] { "System.Collections.Generic" };
        static Regex GenericParamPattern = new Regex(@"`\d", RegexOptions.None);
        public static string RemoveRef(string s, bool removearray = true)
        {
            if (s.EndsWith("&")) s = s.Substring(0, s.Length - 1);
            if (s.EndsWith("[]") && removearray) s = s.Substring(0, s.Length - 2);
            if (s.StartsWith(prefix[0])) s = s.Substring(prefix[0].Length + 1, s.Length - prefix[0].Length - 1);

            s = s.Replace("+", ".");
            if (s.Contains("`"))
            {
                s = GenericParamPattern.Replace(s, "");
                s = s.Replace("[", "<");
                s = s.Replace("]", ">");
            }
            return s;
        }

        public static string TypeDecl(ParameterInfo[] pars, int paraOffset = 0)
        {
            string ret = "";
            for (int n = paraOffset; n < pars.Length; n++)
            {
                ret += ",typeof(";
                if (pars[n].IsOut)
                    ret += "MRubyOut";
                else
                    ret += SimpleTypeName(pars[n].ParameterType);
                ret += ")";
            }
            return ret;
        }

        public static string TypeDecl(Type t)
        {
            if (t.IsGenericType)
            {
                string ret = Naming.GenericBaseName(t);

                string gs = "";
                gs += "<";
                Type[] types = t.GetGenericArguments();
                for (int n = 0; n < types.Length; n++)
                {
                    gs += TypeDecl(types[n]);
                    if (n < types.Length - 1)
                        gs += ",";
                }
                gs += ">";

                ret = Regex.Replace(ret, @"`\d", gs);

                return ret;
            }
            if (t.IsArray)
            {
                return TypeDecl(t.GetElementType()) + "[]";
            }
            else
                return TypeUtil.RemoveRef(t.ToString(), false);
        }

    }

}
