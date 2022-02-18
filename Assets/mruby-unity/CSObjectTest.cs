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



    public class Character_CSObject : CSObject
    {
        static Dictionary<int, object> cache = new Dictionary<int, object>();
        static RClass cls;
        readonly Character obj;

        public Character_CSObject(MrbState mrb, Character _obj)
        {
            obj = _obj;
            var val = DLL.mrb_obj_new(mrb.mrb, cls, 0, null);
            var sym = DLL.mrb_intern_cstr(mrb.mrb, "val");
            var id = RuntimeHelpers.GetHashCode(obj);
            cache.Add(id, obj);
            DLL.mrb_iv_set(mrb.mrb, val, sym, new Value(id).val);
        }

        public static void RegisterClass(MrbState _mrb)
        {
            mrb_state mrb = _mrb.mrb;
            RClass cls = DLL.mrb_define_class(mrb, "Character", DLL.mrb_class_get(mrb, "Object"));
            DLL.mrb_define_method(mrb, cls, "initialize", new DLL.mrb_func_t(_initialize), DLL.MRB_ARGS_REQ(2));
            DLL.mrb_define_method(mrb, cls, "show", _show, DLL.MRB_ARGS_NONE());

        }

        static object getObject(mrb_state mrb, mrb_value obj)
        {
            var id = (int)DLL.mrb_as_int(mrb, obj);
            if( cache.TryGetValue(id, out object found))
            {
                return found;
            }
            else
            {
                return null;
            }
        }

        static bool tryGetObject(mrb_state mrb, mrb_value obj, out object found)
        {
            var id = (int)DLL.mrb_as_int(mrb, obj);
            return cache.TryGetValue(id, out found);
        }

        unsafe public static mrb_value _initialize(mrb_state mrb, mrb_value _self)
        {
            mrb_value* args = DLL.mrb_get_argv(mrb);
            Character obj = new Character("hoge", (int)DLL.mrb_as_int(mrb, args[1]));

            var sym = DLL.mrb_intern_cstr(mrb, "val");
            var id = RuntimeHelpers.GetHashCode(obj);
            cache.Add(id, obj);
            DLL.mrb_iv_set(mrb, _self, sym, new Value(id).val);

            Debug.Log("hoge");

            return DLL.mrb_nil_value();
        }

        public static mrb_value _show(mrb_state mrb, mrb_value self)
        {
            var sym = DLL.mrb_intern_cstr(mrb, "val");
            var v = (int)DLL.mrb_as_int(mrb, DLL.mrb_iv_get(mrb, self, sym));
            var obj = (Character)cache[v];
            Debug.Log(obj);
            var result = obj.Show();

            return DLL.mrb_str_new_cstr(mrb, result);
        }


    }
}
