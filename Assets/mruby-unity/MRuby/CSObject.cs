using System;
using System.Reflection;
using System.Collections.Generic;
#if !SLUA_STANDALONE
using UnityEngine;
#endif

namespace MRuby
{
    public enum MRubyType : int
    {
        LUA_TNONE = -1,
        LUA_TNIL = 0,
        LUA_TBOOLEAN = 1,
        LUA_TLIGHTUSERDATA = 2,
        LUA_TNUMBER = 3,
        LUA_TSTRING = 4,
        LUA_TTABLE = 5,
        LUA_TFUNCTION = 6,
        LUA_TUSERDATA = 7,
        LUA_TTHREAD = 8,
    }


    public class CSObject
    {
        public mrb_value val;
        readonly object obj;

        public CSObject(mrb_state mrb, object _obj)
        {
            obj = _obj;
        }

        public CSObject(mrb_state mrb, object _obj, mrb_value _val)
        {
            obj = _obj;
            val = _val;
        }

        public static bool isImplByLua(Type t)
        {
#if !SLUA_STANDALONE
            return t == typeof(Color)
                || t == typeof(Vector2)
                || t == typeof(Vector3)
                || t == typeof(Vector4)
                || t == typeof(Quaternion);
#else
		    return false;
#endif
        }


        static protected Dictionary<MethodBase, string> methodDict = new Dictionary<MethodBase, string>();


        static protected string GetMethodName(MethodBase method)
        {
            string result = "";

            if (!methodDict.TryGetValue(method, out result))
            {
                Type classType = method.ReflectedType;
                result = string.Format("{0}.{1}", classType.Name, method.Name);
                methodDict.Add(method, result);
            }
            return result;
        }

	}
}
