using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            //
        }
    }

    public class DoNotToLuaAttribute : System.Attribute
    {
        public DoNotToLuaAttribute()
        {
            //
        }
    }

    public class LuaBinderAttribute : System.Attribute
    {
        public int order;
        public LuaBinderAttribute(int order)
        {
            this.order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class StaticExportAttribute : System.Attribute
    {
        public StaticExportAttribute()
        {
            //
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


    public struct Value
    {
        public readonly mrb_value val;

        public Value(mrb_value _val)
        {
            val = _val;
        }

        public Value(object _val) : this(null, _val) { }

        public Value(mrb_state mrb, object _val)
        {
            switch (_val)
            {
                case Value v:
                    val = v.val;
                    break;
                case mrb_value v:
                    val = v;
                    break;
                case byte v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case UInt16 v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case Int16 v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case UInt32 v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case Int32 v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case bool v:
                    val = DLL.mrb_bool_value(v);
                    break;
                case string v:
                    val = DLL.mrb_str_new_cstr(mrb, v);
                    break;
                case float v:
                    val = DLL.mrb_float_value(mrb, v);
                    break;
                case double v:
                    val = DLL.mrb_float_value(mrb, v);
                    break;
                case CSObject o:
                    throw new Exception();
#if false
                    var obj = ObjectCache.GetObject(mrb, o.val);
                    if( obj != null)
                    {

                    }
                        break;
#endif
                default:
                    if (ObjectCache.TryToValue(mrb, _val, out var mrb_v))
                    {
                        val = mrb_v;
                    }
                    else  if (TypeCache.TryGetClass(_val.GetType(), out TypeCache.ConstructorFunc constructor))
                    {
#if true
                        var obj = constructor(mrb, _val);
                        val = obj.val;
#else
                        val = default;
#endif
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                    break;
            }
        }

        public Value(MrbState mrb, object _val)
        {
            switch( _val)
            {
                case Value v:
                    val = v.val;
                    break;
                case mrb_value v:
                    val = v;
                    break;
                case byte v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case UInt16 v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case Int16 v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case UInt32 v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case Int32 v:
                    val = DLL.mrb_fixnum_value(v);
                    break;
                case bool v:
                    val = DLL.mrb_bool_value(v);
                    break;
                case string v:
                    if (mrb == null) throw new ArgumentException();
                    val = DLL.mrb_str_new_cstr(mrb.mrb, v);
                    break;
                case float v:
                    if (mrb == null) throw new ArgumentException();
                    val = DLL.mrb_float_value(mrb.mrb, v);
                    break;
                case double v:
                    if (mrb == null) throw new ArgumentException();
                    val = DLL.mrb_float_value(mrb.mrb, v);
                    break;
                default:
                    if( _val == null)
                    {
                        val = DLL.mrb_nil_value();
                    }
                    else if (ObjectCache.TryToValue(mrb.mrb,  _val, out var mrb_v))
                    {
                        val = mrb_v;
                    }
                    else if (TypeCache.TryGetClass(_val.GetType(), out TypeCache.ConstructorFunc constructor))
                    {
                        if (mrb == null) throw new ArgumentException();
                        var obj = constructor(mrb.mrb, _val);
                        val = obj.val;
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                    break;
            }
        }

        static mrb_value[] argsCache = new mrb_value[16];

        public Value Send(MrbState mrb, string methodName)
        {
            return new Value(DLL.mrb_funcall_argv(mrb.mrb, val, methodName, 0, null));
        }

        public Value Send(MrbState mrb, string methodName, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                argsCache[i] = new Value(mrb, args[i]).val;
            }
            return new Value(DLL.mrb_funcall_argv(mrb.mrb, val, methodName, args.Length, argsCache));
        }

        public Value Send(MrbState mrb, string methodName, params Value[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                argsCache[i] = args[i].val;
            }
            return new Value(DLL.mrb_funcall_argv(mrb.mrb, val, methodName, args.Length, argsCache));
        }

        public string ToString(MrbState mrb)
        {
            return Send(mrb, "to_s").AsString(mrb);
        }

        public string AsString(MrbState mrb)
        {
            var len = DLL.mrb_string_len(mrb.mrb, val);
            var buf = new byte[len];
            DLL.mrb_string_buf(mrb.mrb, val, buf, len);
            return System.Text.Encoding.UTF8.GetString(buf);
        }

        public Int64 AsInteger(MrbState mrb)
        {
            return DLL.mrb_as_int(mrb.mrb, val);
        }

    }

    public class MrbState: IDisposable
    {
        bool disposed;
        internal mrb_state mrb;

        public MrbState()
        {
            mrb = DLL.mrb_open();
            DLL.mrb_unity_set_abort_func(mrb, abortCallback);

            Converter.sym_objid = DLL.mrb_intern_cstr(mrb, "objid");

            var kernel = DLL.mrb_module_get(mrb, "Kernel");
            DLL.mrb_define_module_function(mrb, kernel, "require", MRubyUnity.Core._require, DLL.MRB_ARGS_REQ(1));

            MRubySvr svr = new MRubySvr(this);
            MRubySvr.doBind(mrb);

            DLL.mrb_load_string(mrb, prelude);
        }

        static void abortCallback(string msg)
        {
            throw new Exception(msg);
        }

        public void check()
        {
            if( disposed)
            {
                throw new InvalidOperationException("mrb_state already disposed");
            }
        }

        public void Dispose()
        {
            if( !disposed )
            {
                DLL.mrb_close(mrb);
                disposed = true;
            }
        }

        public Value LoadString(string src)
        {
            return new Value(DLL.mrb_load_string(mrb, src));
        }


        public static string prelude = @"
class LoadError < Exception
end

$stdout = MRubyUnity::Console.new

module Kernel
  def puts(*args)
    $stdout.write args.join(""\\n"")
  end

  def print(*args)
    $stdout.write args.join("" "")
  end
end

module Kernel
  def p(*args)
    args.each { |x| puts x.inspect }
    end
  end
";

    }

}

