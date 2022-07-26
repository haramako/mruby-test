using System;
using System.Collections.Generic;
using System.Text;

namespace MRuby
{
    public struct Value
    {
        public readonly mrb_state mrb;
        public readonly mrb_value val;

        public Value(mrb_state _mrb, mrb_value _val)
        {
            mrb = _mrb;
            val = _val;
        }

        //public Value(object _val) : this(null, _val) { }

        public Value(MrbState _mrb, object _val) : this(_mrb.mrb, _val) { }

        public Value(mrb_state _mrb, object _val)
        {
            mrb = _mrb;
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
                default:
                    if (_val == null)
                    {
                        val = DLL.mrb_nil_value();
                    }
                    else if (ObjectCache.TryToValue(mrb, _val, out var mrb_v))
                    {
                        val = mrb_v;
                    }
                    else if (TypeCache.TryGetClass(_val.GetType(), out TypeCache.ConstructorFunc constructor))
                    {
                        var obj = constructor(_mrb, _val);
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

        public Value Send(string methodName)
        {
            var r = DLL.mrb_funcall_argv(mrb, val, methodName, 0, null);
            var exc = DLL.mrb_mrb_state_exc(mrb);
            if (!exc.IsNil)
            {
                DLL.mrb_mrb_state_clear_exc(mrb);
                throw new RubyException(mrb, exc);
            }
            else
            {
                return new Value(mrb, r);
            }
        }

        public Value Send(string methodName, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                argsCache[i] = new Value(mrb, args[i]).val;
            }
            var r = DLL.mrb_funcall_argv(mrb, val, methodName, args.Length, argsCache);

            var exc = DLL.mrb_mrb_state_exc(mrb);
            if (!exc.IsNil)
            {
                DLL.mrb_mrb_state_clear_exc(mrb);
                throw new RubyException(mrb, exc);
            }
            else
            {
                return new Value(mrb, r);
            }
        }

        public Value Send(string methodName, params Value[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                argsCache[i] = args[i].val;
            }
            var r = DLL.mrb_funcall_argv(mrb, val, methodName, args.Length, argsCache);

            var exc = DLL.mrb_mrb_state_exc(mrb);
            if (!exc.IsNil)
            {
                DLL.mrb_mrb_state_clear_exc(mrb);
                throw new RubyException(mrb, exc);
            }
            else
            {
                return new Value(mrb, r);
            }
        }

        public override string ToString()
        {
            return Send("to_s").AsString();
        }

        public string AsString()
        {
            var len = DLL.mrb_string_len(mrb, val);
            var buf = new byte[len];
            DLL.mrb_string_buf(mrb, val, buf, len);
            return System.Text.Encoding.UTF8.GetString(buf);
        }

        public Int64 AsInteger()
        {
            return DLL.mrb_as_int(mrb, val);
        }

        public Int64 ToInteger()
        {
            if (DLL.mrb_type(val) == mrb_vtype.MRB_TT_INTEGER)
            {
                return DLL.mrb_as_int(mrb, val);
            }
            else
            {
                return Send("to_i").AsInteger();
            }
        }

        public float AsFloat()
        {
            return (float)DLL.mrb_as_float(mrb, val);
        }

        public double AsDouble()
        {
            return DLL.mrb_as_float(mrb, val);
        }

    }

}
