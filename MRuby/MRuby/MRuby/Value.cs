using System;
using System.Collections.Generic;
using System.Text;

namespace MRuby
{
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
                    else if (TypeCache.TryGetClass(_val.GetType(), out TypeCache.ConstructorFunc constructor))
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
                    if (_val == null)
                    {
                        val = DLL.mrb_nil_value();
                    }
                    else if (ObjectCache.TryToValue(mrb.mrb, _val, out var mrb_v))
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

}
