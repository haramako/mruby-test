using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MRuby
{
    public class TypeCache
    {
        VM mrb;
        public delegate mrb_value ConstructorFunc(mrb_state mrb, object obj);
        Dictionary<Type, ConstructorFunc> cache = new Dictionary<Type, ConstructorFunc>();

        public TypeCache(VM _mrb)
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
        VM _mrb;

        /// <summary>
        /// Cache from mrb_value to C# object.
        /// </summary>
        Dictionary<int, object> cache = new Dictionary<int, object>();

        /// <summary>
        /// Cache from C# object to mrb_value.
        /// </summary>
        Dictionary<object, mrb_value> csToMRubyCache = new Dictionary<object, mrb_value>();

        public ObjectCache(VM mrb)
        {
            _mrb = mrb;
        }

        public int AddObject(object obj, mrb_value v)
        {
            var id = RuntimeHelpers.GetHashCode(obj);
            DLL.mrb_gc_register(_mrb.mrb, v);
            cache.Add(id, obj);
            csToMRubyCache.Add(obj, v);
            return id;
        }

        public mrb_value NewObject(mrb_state mrb, mrb_value cls, object obj)
        {
            //CodeGen.Logger.Log($"NewObject");
            var val = DLL.mrb_funcall_argv(mrb, cls, "allocate", 0, null);
            var id = AddObject(obj, val);
            DLL.mrb_iv_set(mrb, val, _mrb.SymObjID, DLL.mrb_fixnum_value(id));
            //CodeGen.Logger.Log($"NewObject: {val.val} as {obj} {id}");
            return val;
        }

        public mrb_value NewObjectByVal(mrb_state mrb, mrb_value self, object obj)
        {
            //CodeGen.Logger.Log($"NewObjVal");
            var id = AddObject(obj, self);
            DLL.mrb_iv_set(mrb, self, _mrb.SymObjID, DLL.mrb_fixnum_value(id));
            //CodeGen.Logger.Log($"NewObjVal: {self.val} as {obj} {id}");
            return self;
        }

        public object GetObject(mrb_state mrb, mrb_value val)
        {
            //CodeGen.Logger.Log($"GetObject: {val.val} ");
            var id = (int)DLL.mrb_as_int(mrb, DLL.mrb_iv_get(mrb, val, _mrb.SymObjID));
            var obj = cache[id];
            //CodeGen.Logger.Log($"GetObject: {val.val} to {obj} {id}");
            return obj;
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

    public class ValueCache
    {
        VM mrb;

        /// <summary>
        /// Cache from mrb_value to C# object.
        /// </summary>
        HashSet<WeakReference<Value>> cache = new HashSet<WeakReference<Value>>();

        public ValueCache(VM _mrb)
        {
            mrb = _mrb;
        }

        public void AddValue(Value v)
        {
            cache.Add(new WeakReference<Value>(v));
        }

        public void Clear()
        {
            foreach (var reference in cache)
            {
                if (reference.TryGetTarget(out var val))
                {
                    val.mrb.val = UIntPtr.Zero;
                }
            }
        }
    }
}
