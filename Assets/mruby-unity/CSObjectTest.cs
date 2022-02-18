using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;

namespace MRuby
{
    public class Character
    {
        public string Name;
        public int Age;

        public Character(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public string Show()
        {
            return Name + ":" + Age;
        }
    }

    public class TypeCache
    {
        public delegate CSObject ConstructorFunc(MrbState mrb, object obj);
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
            var id = (int)DLL.mrb_as_int(mrb, obj);
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
            var id = (int)DLL.mrb_as_int(mrb, obj);
            return cache.TryGetValue(id, out found);
        }

    }

    public class MRuby_Character : CSObject
    {
        static RClass cls;
        static mrb_value cls_value;
        static mrb_sym sym_objid;
        readonly Character obj;

        public MRuby_Character(MrbState mrb, object _obj)
        {
            obj = (Character)_obj;
            var id = ObjectCache.AddObject(obj);

            val = new Value(cls_value).Send(mrb, "allocate").val;
            DLL.mrb_iv_set(mrb.mrb, val, sym_objid, DLL.mrb_fixnum_value(id));
        }

        static CSObject Construct(MrbState mrb, object obj) => new MRuby_Character(mrb, obj);

        public static void RegisterClass(MrbState _mrb)
        {
            mrb_state mrb = _mrb.mrb;
            cls = DLL.mrb_define_class(mrb, "Character", DLL.mrb_class_get(mrb, "Object"));
            cls_value = DLL.mrb_obj_value(cls.val);
            DLL.mrb_define_method(mrb, cls, "initialize", new DLL.mrb_func_t(_initialize), DLL.MRB_ARGS_REQ(2));
            DLL.mrb_define_method(mrb, cls, "show", _show, DLL.MRB_ARGS_NONE());

            sym_objid = DLL.mrb_intern_cstr(mrb, "objid");

            TypeCache.AddType(typeof(Character), Construct);
        }

        unsafe public static mrb_value _initialize(mrb_state mrb, mrb_value _self)
        {
            mrb_value* args = DLL.mrb_get_argv(mrb);
            var name = DLL.mrb_as_string(mrb, args[0]);
            var age = (int)DLL.mrb_as_int(mrb, args[1]);
            Character obj = new Character(name, age);

            var id = ObjectCache.AddObject(obj);
            DLL.mrb_iv_set(mrb, _self, sym_objid, new Value(id).val);

            return DLL.mrb_nil_value();
        }

        public static mrb_value _show(mrb_state mrb, mrb_value self)
        {
            var v = (int)DLL.mrb_as_int(mrb, DLL.mrb_iv_get(mrb, self, sym_objid));

            var obj = (Character)ObjectCache.GetObject(v);

            var result = obj.Show();

            return DLL.mrb_str_new_cstr(mrb, result);
        }


    }
}
