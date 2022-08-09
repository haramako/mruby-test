using System;

namespace MRuby
{
    public class Value
    {
        public static int FinalyzerCount;

        public mrb_state mrb;
        public readonly mrb_value val;

        static mrb_value[] argsCache = new mrb_value[16]; // TODO: multithread

        public Value(mrb_state _mrb, mrb_value _val)
        {
            mrb = _mrb;
            val = _val;
            DLL.mrb_gc_register(mrb, val);
            VM.FindCache(mrb).ValueCache.AddValue(this);
        }

        public Value(object _val) : this(null, _val) { }

        public Value(VM _mrb, object _val) : this(_mrb.mrb, _val) { }

        public Value(mrb_state _mrb, object _val)
        {
            mrb = _mrb;
            val = Converter.make_value(_mrb, _val);
            DLL.mrb_gc_register(mrb, val);
            VM.FindCache(mrb).ValueCache.AddValue(this);
        }

        ~Value()
        {
            FinalyzerCount++;
            if (mrb.val != UIntPtr.Zero)
            {
                DLL.mrb_gc_unregister(mrb, val);
            }
        }

        public Value x(string methodName) => Send(methodName);
        public Value x(string methodName, params object[] args) => Send(methodName, args);
        public Value x(string methodName, params Value[] args) => Send(methodName, args);

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

        public bool AsBool()
        {
            return DLL.mrb_bool(val);
        }

        public object AsObject()
        {
            return Converter.checkVar(mrb, val);
        }

    }

}
