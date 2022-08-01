using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;

namespace MRuby
{
    public class TypeCache
    {
        MrbState mrb;
        public delegate mrb_value ConstructorFunc(mrb_state mrb, object obj);
        static Dictionary<Type, ConstructorFunc> cache = new Dictionary<Type, ConstructorFunc>();

        public TypeCache(MrbState _mrb)
        {
            mrb = _mrb;
        }

        public void AddType(Type type, ConstructorFunc cls)
        {
            cache[type] = cls;
        }

        public ConstructorFunc GetClass(Type type)
        {
            return cache[type];
        }

        public bool TryGetClass(Type type, out ConstructorFunc constructor)
        {
            if (cache.TryGetValue(type, out constructor))
            {
                return true;
            }
            else
            {
                return TryGetClass(type.BaseType, out constructor);
            }
        }
    }

    public class ObjectCache
    {
        MrbState _mrb;

        static Dictionary<int, object> cache = new Dictionary<int, object>();
        static Dictionary<object, mrb_value> csToMRubyCache = new Dictionary<object, mrb_value>();

        public ObjectCache(MrbState mrb)
        {
            _mrb = mrb;
        }

        public int AddObject(object obj, mrb_value v)
        {
            var id = RuntimeHelpers.GetHashCode(obj);
            cache.Add(id, obj);
            csToMRubyCache.Add(obj, v);
            return id;
        }

        public mrb_value NewObject(mrb_state mrb, mrb_value cls, object obj)
        {
            var val = DLL.mrb_funcall_argv(mrb, cls, "allocate", 0, null);
            var id = AddObject(obj, val);
            DLL.mrb_iv_set(mrb, val, _mrb.SymObjID, DLL.mrb_fixnum_value(id));
            return val;
        }

        public mrb_value NewObjectByVal(mrb_state mrb, mrb_value self, object obj)
        {
            var id = AddObject(obj, self);
            DLL.mrb_iv_set(mrb, self, _mrb.SymObjID, DLL.mrb_fixnum_value(id));
            return self;
        }

        public object GetObject(mrb_state mrb, mrb_value obj)
        {
            //UnityEngine.Debug.Log("GetObject: " + obj.val);
            var x = DLL.mrb_iv_get(mrb, obj, _mrb.SymObjID);
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

        public object GetObject(int id)
        {
            return cache[id];
        }

        public bool TryGetObject(mrb_state mrb, mrb_value obj, out object found)
        {
            var id = (int)DLL.mrb_as_int(mrb, DLL.mrb_iv_get(mrb, obj, _mrb.SymObjID));
            return cache.TryGetValue(id, out found);
        }


        public bool TryToValue(mrb_state mrb, object obj, out mrb_value found)
        {
            return csToMRubyCache.TryGetValue(obj, out found);
        }

    }
}
