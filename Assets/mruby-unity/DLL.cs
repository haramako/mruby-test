using System;
using System.Runtime.InteropServices;

using mrb_int = System.Int64;

namespace MRuby
{
    public struct mrb_state
    {
        public UIntPtr val;
    }

    public struct mrb_value
    {
        public UInt64 val;
    }

    public struct RClass
    {
        public UIntPtr val;
    }

    public struct mrb_aspec
    {
        public UInt64 val;
        public mrb_aspec(UInt64 n)
        {
            val = n;
        }
    }

    public struct mrb_sym
    {
        public UInt32 val;
    }

    public struct mrbc_context
    {
        public UIntPtr val;
    }



#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate mrb_value LuaCSFunction(mrb_state mrb, mrb_value _self);
#else
	public delegate mrb_value LuaCSFunction(mrb_state mrb, mrb_value _self);
#endif

    public static class DLL
    {
        const string Dll = "mruby.dll";

        public delegate mrb_value mrb_func_t(mrb_state mrb, mrb_value self);

        public static mrb_aspec MRB_ARGS_REQ(int n) => new mrb_aspec(((UInt64)n & 0x1fUL) << 18);
        public static mrb_aspec MRB_ARGS_OPT(int n) => new mrb_aspec(((UInt64)n & 0x1fUL) << 13);
        public static mrb_aspec MRB_ARGS_NONE() => new mrb_aspec(0);

#region Ruby original
        [DllImport(Dll)]
        public static extern mrb_state mrb_open();

        [DllImport(Dll)]
        public static extern void mrb_close(mrb_state mrb);

        [DllImport(Dll)]
        public static extern mrb_value mrb_load_string(mrb_state mrb, string s);

        [DllImport(Dll)]
        public static extern mrb_value mrb_load_string_cxt(mrb_state mrb, string s, mrbc_context cxt);

        [DllImport(Dll)]
        public static extern mrb_int mrb_get_argc(mrb_state mrb);

        [DllImport(Dll)]
        public unsafe static extern mrb_value* mrb_get_argv(mrb_state mrb);

        [DllImport(Dll)]
        public static extern RClass mrb_define_class(mrb_state mrb, string name, RClass super);

        [DllImport(Dll)]
        public static extern void mrb_define_class_method(mrb_state mrb, RClass c, string name, mrb_func_t func, mrb_aspec aspec);

        [DllImport(Dll)]
        public static extern void mrb_define_method(mrb_state mrb, RClass c, string name, mrb_func_t func, mrb_aspec aspec);

        [DllImport(Dll)]
        public static extern RClass mrb_class_get(mrb_state mrb, string name);

        [DllImport(Dll)]
        public static extern mrb_value mrb_obj_new(mrb_state mrb, RClass c, mrb_int argc, mrb_value[] argv);

        [DllImport(Dll)]
        public static extern mrb_sym mrb_intern_cstr(mrb_state mrb, string str);

        [DllImport(Dll)]
        public static extern void mrb_iv_set(mrb_state mrb, mrb_value obj, mrb_sym sym, mrb_value v);

        [DllImport(Dll)]
        public static extern mrb_value mrb_iv_get(mrb_state mrb, mrb_value obj, mrb_sym sym);

        [DllImport(Dll)]
        public static extern mrb_value mrb_str_new_cstr(mrb_state mrb, string str);

        [DllImport(Dll)]
        public static extern string mrb_string_cstr(mrb_state mrb, mrb_value str);

        [DllImport(Dll)]
        public static extern mrbc_context mrbc_context_new(mrb_state mrb);

        [DllImport(Dll)]
        public static extern void mrbc_context_free(mrb_state mrb, mrbc_context cxt);


        #endregion

        #region Value creation
        [DllImport(Dll, EntryPoint = "mrb_unity_fixnum_value")]
        public static extern mrb_value mrb_fixnum_value(mrb_int v);

        [DllImport(Dll, EntryPoint = "mrb_unity_int_value")]
        public static extern mrb_value mrb_int_value(mrb_state mrb, mrb_int v);

        [DllImport(Dll, EntryPoint = "mrb_unity_float_value")]
        public static extern mrb_value mrb_float_value(mrb_state mrb, double v);

        [DllImport(Dll, EntryPoint = "mrb_unity_nil_value")]
        public static extern mrb_value mrb_nil_value();

        [DllImport(Dll, EntryPoint = "mrb_unity_false_value")]
        public static extern mrb_value mrb_false_value();

        [DllImport(Dll, EntryPoint = "mrb_unity_true_value")]
        public static extern mrb_value mrb_true_value();

        [DllImport(Dll, EntryPoint = "mrb_unity_bool_value")]
        public static extern mrb_value mrb_bool_value(bool v);

        [DllImport(Dll, EntryPoint = "mrb_unity_obj_value")]
        public static extern mrb_value mrb_obj_value(UIntPtr p);

        [DllImport(Dll, EntryPoint = "mrb_unity_mrb_state_exc")]
        public static extern mrb_value mrb_mrb_state_exc(mrb_state mrb);

        [DllImport(Dll, EntryPoint = "mrb_unity_nil_p")]
        public static extern bool mrb_nil_p(mrb_value v);

        #endregion

        #region Value conversion
        [DllImport(Dll, EntryPoint = "mrb_unity_as_int")]
        public static extern Int64 mrb_as_int(mrb_state mrb, mrb_value obj);

        [DllImport(Dll, EntryPoint = "mrb_unity_string_len")]
        public static extern Int64 mrb_string_len(mrb_state mrb, mrb_value obj);

        [DllImport(Dll, EntryPoint = "mrb_unity_string_buf")]
        public static extern Int64 mrb_string_buf(mrb_state mrb, mrb_value obj, byte[] buf, mrb_int buf_len);

        public static string mrb_as_string(mrb_state mrb, mrb_value str)
        {
            var len = DLL.mrb_string_len(mrb, str);
            var buf = new byte[len];
            DLL.mrb_string_buf(mrb, str, buf, len);
            return System.Text.Encoding.UTF8.GetString(buf);
        }
#endregion

#region Others
        public delegate void AbortFunc(string msg);

        [DllImport(Dll)]
        public static extern void mrb_unity_set_abort_func(mrb_state mrb, AbortFunc f);

        [DllImport(Dll, EntryPoint = "mrb_unity_funcall_argv")]
        public static extern mrb_value mrb_funcall_argv(mrb_state mrb, mrb_value val, string name, mrb_int argc, mrb_value[] vals);
#endregion
    }



}
