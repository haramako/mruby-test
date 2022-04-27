using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;

namespace MRuby
{
    public class TypeCache
    {
        public delegate CSObject ConstructorFunc(mrb_state mrb, object obj);
        static Dictionary<Type, ConstructorFunc> cache = new Dictionary<Type, ConstructorFunc>();

        public static void AddType(Type type, ConstructorFunc cls)
        {
            cache.Add(type, cls);
        }

        public static ConstructorFunc GetClass(Type type)
        {
            return cache[type];
        }

        public static bool TryGetClass(Type type, out ConstructorFunc constructor)
        {
            return cache.TryGetValue(type, out constructor);
        }
    }

    public class ObjectCache
    {
        static Dictionary<int, object> cache = new Dictionary<int, object>();

        public static int AddObject(object obj)
        {
            var id = RuntimeHelpers.GetHashCode(obj);
            cache.Add(id, obj);
            return id;
        }

        public static object GetObject(mrb_state mrb, mrb_value obj)
        {
            //UnityEngine.Debug.Log("GetObject: " + obj.val);
            var x = DLL.mrb_iv_get(mrb, obj, Converter.sym_objid);
            var id = (int)DLL.mrb_as_int(mrb, x);
            if (cache.TryGetValue(id, out object found))
            {
                return found;
            }
            else
            {
                return null;
            }
        }

        public static object GetObject(int id)
        {
            return cache[id];
        }

        public static bool TryGetObject(mrb_state mrb, mrb_value obj, out object found)
        {
            var id = (int)DLL.mrb_as_int(mrb, DLL.mrb_iv_get(mrb, obj, Converter.sym_objid));
            return cache.TryGetValue(id, out found);
        }

    }
}
