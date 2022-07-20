using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MRuby
{
    public class MrbState : IDisposable
    {
        bool disposed;
        public mrb_state mrb;

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
            if (disposed)
            {
                throw new InvalidOperationException("mrb_state already disposed");
            }
        }

        public void Dispose()
        {
            if (!disposed)
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

# $stdout = MRubyUnity::Console.new

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

