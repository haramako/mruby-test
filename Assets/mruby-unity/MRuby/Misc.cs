﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MRuby
{
    #region Attributes
#pragma warning disable 414
    public class MonoPInvokeCallbackAttribute : System.Attribute
    {
        private Type type;
        public MonoPInvokeCallbackAttribute(Type t)
        {
            type = t;
        }
    }
#pragma warning restore 414

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Interface)]
    public class CustomMRubyClassAttribute : System.Attribute
    {
        public CustomMRubyClassAttribute()
        {
        }
    }

    public class DoNotToLuaAttribute : System.Attribute
    {
        public DoNotToLuaAttribute()
        {
        }
    }

    public class LuaBinderAttribute : System.Attribute
    {
        public LuaBinderAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class StaticExportAttribute : System.Attribute
    {
        public StaticExportAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class LuaOverrideAttribute : System.Attribute
    {
        public string fn;
        public LuaOverrideAttribute(string fn)
        {
            this.fn = fn;
        }
    }

    public class OverloadLuaClassAttribute : System.Attribute
    {
        public OverloadLuaClassAttribute(Type target)
        {
            targetType = target;
        }
        public Type targetType;
    }
    #endregion

    public class RuntimeClassDesc
    {
        public readonly string RubyName;
        public readonly Action<mrb_state> BinderFunc;
        public readonly string BaseTypeRubyName;

        public RuntimeClassDesc(string rubyName, Action<mrb_state> binderFunc, string baseTypeRubyName)
        {
            RubyName = rubyName;
            BinderFunc = binderFunc;
            BaseTypeRubyName = baseTypeRubyName;
        }
    }

    public static class Binder
    {
        class Entry
        {
            public RuntimeClassDesc Desc;
            public bool Registered;
        }

        public static bool LogEnabled = false;

        public static void Bind(MrbState mrb, params IList<RuntimeClassDesc>[] lists)
        {
            var dict = new Dictionary<string, Entry>();
            foreach (var list in lists)
            {
                foreach (var desc in list)
                {
                    dict[desc.RubyName] = new Entry() { Desc = desc };
                }
            }

            foreach (var entry in dict.Values)
            {
                if (entry.Registered)
                {
                    continue;
                }

                bindOne(mrb, dict, entry);
            }

        }

        static void bindOne(MrbState _mrb, Dictionary<string, Entry> dict, Entry entry)
        {
            var desc = entry.Desc;
            var mrb = _mrb.mrb;
            if (entry.Registered)
            {
                return;
            }

            var (ns, name) = splitName(desc.RubyName);

            if (ns != null)
            {
                bindOne(_mrb, dict, dict[ns]);
            }

            if (desc.BaseTypeRubyName != null)
            {
                bindOne(_mrb, dict, dict[desc.BaseTypeRubyName]);
            }


            //Console.WriteLine($"Bind {desc.RubyName}");
            if (desc.BinderFunc == null)
            {
                //Console.WriteLine($"namespace {name}");
                DLL.mrb_define_module_under(mrb, Converter.GetClass(mrb, ns), name);
            }
            else
            {
                string baseType = desc.BaseTypeRubyName ?? "Object";
                //Console.WriteLine($"class {name} {baseType}");
                DLL.mrb_define_class_under(mrb, Converter.GetClass(mrb, ns), name, Converter.GetClass(mrb, baseType));
                desc.BinderFunc?.Invoke(mrb);
            }

            entry.Registered = true;
        }

        static (string, string) splitName(string fullname)
        {
            var idx = fullname.LastIndexOf("::");
            if (idx < 0)
            {
                return (null, fullname);
            }
            else
            {
                return (fullname.Substring(0, idx), fullname.Substring(idx + 2, fullname.Length - idx - 2));
            }
        }
    }
}