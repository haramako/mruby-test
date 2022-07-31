using System;
using System.Collections.Generic;
using System.Text;

namespace MRuby
{
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

}
