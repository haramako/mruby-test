using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MRuby.CodeGen
{
    public static class TypeCond
    {
        public static bool DontExport(MemberInfo mi)
        {
            if (mi is null)
            {
                throw new ArgumentNullException(nameof(mi));
            }

            var methodString = string.Format("{0}.{1}", mi.DeclaringType, mi.Name);
            if (CustomExport.FunctionFilterList.Contains(methodString))
                return true;
            // directly ignore any components .ctor
            if (mi.DeclaringType.IsSubclassOf(typeof(UnityEngine.Component)))
            {
                if (mi.MemberType == MemberTypes.Constructor)
                {
                    return true;
                }
            }

            // Check in custom export function filter list.
#if !SLUA_STANDALONE
            List<object> aFuncFilterList = LuaCodeGen.GetEditorField<ICustomExportPost>("FunctionFilterList");
            foreach (object aFilterList in aFuncFilterList)
            {
                if (((List<string>)aFilterList).Contains(methodString))
                {
                    return true;
                }
            }
#endif

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

            return mi.IsDefined(typeof(DoNotToLuaAttribute), false);
        }

        public static bool ContainUnsafe(MethodBase mi)
        {
            foreach (ParameterInfo p in mi.GetParameters())
            {
                if (p.ParameterType.FullName.Contains("*"))
                    return true;
            }
            return false;
        }

        public static bool IsPInvoke(MethodInfo mi, out bool instanceFunc)
        {
            if (mi.IsDefined(typeof(MRuby.MonoPInvokeCallbackAttribute), false))
            {
                instanceFunc = !mi.IsDefined(typeof(StaticExportAttribute), false);
                return true;
            }
            instanceFunc = true;
            return false;
        }

        public static bool IsUsefullMethod(MethodInfo method)
        {
            if (method.Name != "GetType" && method.Name != "GetHashCode" && method.Name != "Equals" &&
                /* method.Name != "ToString" && */ method.Name != "Clone" &&
                method.Name != "GetEnumerator" && method.Name != "CopyTo" &&
                method.Name != "op_Implicit" && method.Name != "op_Explicit" &&
                !method.Name.StartsWith("get_", StringComparison.Ordinal) &&
                !method.Name.StartsWith("set_", StringComparison.Ordinal) &&
                !method.Name.StartsWith("add_", StringComparison.Ordinal) &&
                !IsObsolete(method) && !method.ContainsGenericParameters &&
                method.ToString() != "Int32 Clamp(Int32, Int32, Int32)" &&
                !method.Name.StartsWith("remove_", StringComparison.Ordinal))
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
    }

}
