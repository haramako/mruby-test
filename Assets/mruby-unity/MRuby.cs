using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        public Value(MrbState mrb, object _val)
        {
            switch( _val)
            {
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
                    throw new ArgumentException();
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
                argsCache[i] = new Value(args[i]).val;
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

    }

}

