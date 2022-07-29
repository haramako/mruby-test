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
            val = Converter.make_value(_mrb, _val);
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

        public object AsObject()
        {
            return Converter.checkVar(mrb, val);
        }

    }

}
