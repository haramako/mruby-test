using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MRuby
{
    public class RubyException : Exception
    {
        mrb_state mrb;
        Value exc;

        public RubyException(mrb_state _mrb, mrb_value _exc) : base()
        {
            mrb = _mrb;
            exc = new Value(mrb, _exc);
        }

        public override string Message => exc.ToString();

        public override string StackTrace => exc.Send("backtrace").Send("join", "\n").ToString();

        public Value Exception => exc;
    }

    public class AbortException : RubyException
    {
        public AbortException(mrb_state _mrb, mrb_value _exc) : base(_mrb, _exc) { }
    }


    public class MrbState : IDisposable
    {
        public readonly TypeCache TypeCache;
        public readonly ObjectCache ObjectCache;
        public readonly ValueCache ValueCache;
        public readonly mrb_sym SymObjID;

        bool disposed;
        public mrb_state mrb;

        static Dictionary<UIntPtr, MrbState> mrbStateCache = new Dictionary<UIntPtr, MrbState>();

        public static MrbState FindCache(mrb_state mrb)
        {
            return mrbStateCache[mrb.val];
        }

        public MrbState()
        {
            TypeCache = new TypeCache(this);
            ObjectCache = new ObjectCache(this);
            ValueCache = new ValueCache(this);

            mrb = DLL.mrb_open();
            DLL.mrb_unity_set_abort_func(mrb, abortCallback);

            mrbStateCache.Add(mrb.val, this);

            SymObjID = DLL.mrb_intern_cstr(mrb, "objid");

            var kernel = DLL.mrb_module_get(mrb, "Kernel");
            DLL.mrb_define_module_function(mrb, kernel, "require", MRubyUnity.Core._require, DLL.MRB_ARGS_REQ(1));

            //DLL.mrb_load_string(mrb, prelude);
        }

        static void abortCallback(mrb_state mrb, mrb_value exc)
        {
            throw new AbortException(mrb, exc);
        }

        public void check()
        {
            if (disposed)
            {
                throw new InvalidOperationException("mrb_state already disposed");
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                ValueCache.Clear();
                DLL.mrb_close(mrb);
                disposed = true;
            }
        }

        public Value LoadString(string src)
        {
            var r = DLL.mrb_load_string(mrb, src);

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

        List<MRubyCSFunction> pinned = new List<MRubyCSFunction>();
        public MRubyCSFunction Pin(MRubyCSFunction f)
        {
            pinned.Add(f);
            return f;
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

