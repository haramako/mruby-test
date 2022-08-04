using System;
using System.Collections.Generic;
using System.Linq;

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

    public class VMOption
    {
        public bool BindAutomatically = true;
        public string[] LoadPath = new string[] { "." };
    }


    public class VM : IDisposable
    {
        public static VMOption DefaultOption = new VMOption();

        public readonly TypeCache TypeCache;
        public readonly ObjectCache ObjectCache;
        public readonly ValueCache ValueCache;
        public readonly mrb_sym SymObjID;

        public readonly VMOption Option;

        bool disposed;
        public mrb_state mrb;

        static Dictionary<UIntPtr, VM> mrbStateCache = new Dictionary<UIntPtr, VM>();

        public static VM FindCache(mrb_state mrb)
        {
            return mrbStateCache[mrb.val];
        }

        public VM(VMOption opt = null)
        {
            Option = opt ?? DefaultOption;

            TypeCache = new TypeCache(this);
            ObjectCache = new ObjectCache(this);
            ValueCache = new ValueCache(this);

            mrb = DLL.mrb_open();
            DLL.mrb_unity_set_abort_func(mrb, abortCallback);

            mrbStateCache.Add(mrb.val, this);

            SymObjID = DLL.mrb_intern_cstr(mrb, "objid");

            var kernel = DLL.mrb_module_get(mrb, "Kernel");
            DLL.mrb_define_module_function(mrb, kernel, "require", MRubyUnity.Core._require, DLL.MRB_ARGS_REQ(1));

            MRubyUnity.Core.LoadPath = Option.LoadPath;

            if (Option.BindAutomatically)
            {
                bindAll();
            }

            DLL.mrb_load_string(mrb, prelude);
        }

        static void abortCallback(mrb_state mrb, mrb_value exc)
        {
            throw new AbortException(mrb, exc);
        }

        void check()
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

        public Value Run(string src, string filename = null) => LoadString(src, filename);

        public Value LoadString(string src, string filename = null)
        {
            mrb_value r;
            if (filename != null)
            {
                r = DLL.mrb_load_string_filename(mrb, src, filename);
            }
            else
            {
                r = DLL.mrb_load_string(mrb, src);
            }

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

        /// <summary>
        /// Bind all BindData.
        /// </summary>
        void bindAll()
        {
            var assemblys = AppDomain.CurrentDomain.GetAssemblies();
            var bindList = new List<(Type, RuntimeClassDesc[])>();
            foreach (var assembly in assemblys)
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attr = Attribute.GetCustomAttribute(type, typeof(MRubyBinderAttribute)) as MRubyBinderAttribute;
                    if (attr != null)
                    {
                        var f = type.GetField("BindData");
                        var list = f.GetValue(null) as RuntimeClassDesc[];
                        if (list != null)
                        {
                            bindList.Add((type, list));
                        }
                    }
                }
            }

            CodeGen.Logger.Log("Mruby.VM: Bind " + string.Join(", ", bindList.Select(i => i.Item1)));
            Binder.Bind(this, bindList.Select(i => i.Item2).ToArray());
        }


        static string prelude = @"
class LoadError < Exception
end

$stdout = MRubyUnity::Console.new

module Kernel
  def puts(*args)
    $stdout.write args.join(""\n"")
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

