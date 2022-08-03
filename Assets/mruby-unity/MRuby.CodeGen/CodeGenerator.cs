// The MIT License (MIT)

// Copyright 2015 Siney/Pangweiwei siney@yeah.net
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

// #define ENABLE_PROFILE

namespace MRuby.CodeGen
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class CodeGenerator
    {
        public string givenNamespace;

        ClassDesc cls;
        Registry reg;

        CodeWriter w;

        public CodeGenerator(Registry _reg, ClassDesc _cls, string path)
        {
            reg = _reg;
            cls = _cls;
            w = new CodeWriter(Path.Combine(path, cls.ExportName + ".cs"));
        }

        public void Generate()
        {
            var t = cls.Type;
            WriteHead();
            WriteFunction();
            WriteField();
            WriteRegister();
            WriteEnd();

            cls.Exported = true;
            w.Dispose();
        }

        private void WriteHead()
        {
            w.Write("#if true");
            w.Write("using System;");
            w.Write("using MRuby;");
            w.Write("public class {0} {{", cls.ExportName);

            w.Write("static RClass _cls;");
            w.Write("static mrb_value _clsval;");

            WriteFunctionAttr();
            w.Write("static mrb_value Construct(mrb_state mrb, object obj) => mrb.GetVM().ObjectCache.NewObject(mrb, _clsval, obj);", cls.ExportName);
        }

        private void WriteEnd()
        {
            w.Write("}");
            w.Write("#endif");
        }

        #region Write Register
        void WriteRegister()
        {
#if UNITY_5_3_OR_NEWER
            w.Write( "[UnityEngine.Scripting.Preserve]");
#endif
            var t = cls.Type;

            var fullname = string.IsNullOrEmpty(givenNamespace) ? cls.FullName : givenNamespace;
            var fullnames = fullname.Split('.');

            // Write export function
            w.Write("static public void Register(mrb_state mrb) {");
            w.Write("var _vm = mrb.GetVM();");


            if (t.BaseType != null && t.BaseType.Name.Contains("UnityEvent`"))
            {
                w.Write("MRubyUnityEvent_{1}.reg(mrb);", cls.ExportName, reg.FindByType(cls.BaseType, cls).RubyFullName);
            }

            w.Write("_cls = Converter.GetClass(mrb, \"{0}\");", cls.RubyFullName);
            w.Write("_clsval = DLL.mrb_obj_value(_cls.val);");

            w.Write("mrb.GetVM().TypeCache.AddType(typeof({0}), Construct);", cls.CodeName);

            foreach (var md in cls.MethodDescs.Values)
            {
                var f = md.Name;

                if (md.IsStatic)
                {
                    w.Write("DLL.mrb_define_class_method(mrb, _cls, \"{0}\", _vm.Pin({1}), DLL.MRB_ARGS_OPT(16));", md.RubyName, f); // TODO
                }
                else
                {
                    w.Write("DLL.mrb_define_method(mrb, _cls, \"{0}\", _vm.Pin({1}), DLL.MRB_ARGS_OPT(16));", md.RubyName, f);
                }
            }

            foreach (var f in cls.Fields.Values)
            {
                if (f.CanRead)
                {
                    w.Write("DLL.mrb_define_method(mrb, _cls, \"{0}\", _vm.Pin({1}), DLL.MRB_ARGS_OPT(16));", f.RubyName, f.GetterName);
                }
                if (f.CanWrite)
                {
                    w.Write("DLL.mrb_define_method(mrb, _cls, \"{0}=\", _vm.Pin({1}), DLL.MRB_ARGS_OPT(16));", f.RubyName, f.SetterName);
                }
            }

            w.Write("}");
        }
        #endregion

        #region Write Field
        private void WriteField()
        {
            var t = cls.Type;

            foreach (FieldDesc f in cls.Fields.Values)
            {
                // TODO
                //if (DontExport(fi) || IsObsolete(fi))
                //  continue;

                if (f.Type.BaseType != typeof(MulticastDelegate))
                {
                    WriteFunctionAttr();
                    w.Write("static public mrb_value get_{0}(mrb_state mrb, mrb_value _self) {{", f.Name);
                    WriteFunctionBegin();

                    if (f.IsStatic)
                    {
                        WriteReturn(string.Format("{0}.{1}", TypeUtil.TypeDecl(t), f.Name));
                    }
                    else
                    {
                        WriteCheckSelf();
                        WriteReturn(string.Format("self.{0}", f.Name));
                    }

                    WriteFunctionEnd();
                    w.Write("}");

                }

                if (f.CanWrite)
                {
                    WriteFunctionAttr();
                    w.Write("static public mrb_value set_{0}(mrb_state mrb, mrb_value _self) {{", f.Name);
                    WriteFunctionBegin();
                    if (f.IsStatic)
                    {
                        w.Write("{0} v;", TypeUtil.TypeDecl(f.Type));
                        WriteCheckType(f.Type, 0);
                    }
                    else
                    {
                        WriteCheckSelf();
                        w.Write("{0} v;", TypeUtil.TypeDecl(f.Type));
                        WriteCheckType(f.Type, 0);
                    }
                    w.Write("self.{0} = v;", f.Name);
                    WriteReturn("v");

                    WriteFunctionEnd();
                    w.Write("}");
                }
            }
        }
        #endregion

        #region Generate function

        private void WriteFunction()
        {
            foreach (var m in cls.MethodDescs.Values)
            {
                WriteFunctionAttr();
                w.Write("static public mrb_value {0}(mrb_state mrb, mrb_value _self) {{", m.Name);
                WriteFunctionImpl(m);
            }
        }

        void WriteFunctionImpl(MethodDesc md)
        {
            WriteFunctionBegin();

            if (!md.IsOverloaded) // no override function
            {
                var m = md.Methods[0];
                w.Write("Converter.CheckArgc(_argc, {0}, {1});", m.RequiredParamNum, m.ParamNum);

                WriteFunctionCall(md, m);
            }
            else // 2 or more override function
            {
                bool first = true;
                foreach (var m in md.Methods)
                {
                    var ifCode = first ? "if" : "else if";
                    if (m.Info.MemberType == MemberTypes.Method || m.Info.MemberType == MemberTypes.Constructor)
                    {
                        var mi = m.Info as MethodBase;
                        ParameterInfo[] pars = mi.GetParameters();
                        var requireParameterNum = pars.TakeWhile(p => !p.HasDefaultValue).Count();
                        var argTypes = pars.Select(p => reg.FindByType(p.ParameterType, cls).CodeName).Select(s => $"typeof({s})").ToArray();

                        if (argTypes.Length > 0)
                        {
                            var argTypesStr = string.Join(",", argTypes);
                            w.Write("{0}(_argc >= {2} && _argc <= {3} && Converter.matchType(mrb, _argv, {1})){{", ifCode, argTypesStr, requireParameterNum, pars.Length);
                        }
                        else
                        {
                            w.Write("{0}(_argc == 0 ){{", ifCode, requireParameterNum, pars.Length);
                        }

                        WriteFunctionCall(md, m);
                        w.Write("}");
                        first = false;
                    }
                    else
                    {
                        Logger.LogError($"Unknown method type {m.Name} in {m}");
                    }
                    first = false;
                }

                w.Write("throw new Exception(\"No matched override function {0} to call\");", md.Name);
            }

            WriteFunctionEnd();
            w.Write("}");
        }

        void WriteCheckSelf()
        {
            var t = cls.Type;
            if (t.IsValueType)
            {
                w.Write("{0} self;", TypeUtil.TypeDecl(t));
                if (TypeUtil.IsBaseType(t))
                    w.Write("Converter.checkType(mrb,1,out self);");
                else
                    w.Write("Converter.checkValueType(mrb,1,out self);");
            }
            else
            {
                w.Write("{0} self=({0})Converter.checkSelf(mrb, _self);", TypeUtil.TypeDecl(t));
            }
        }

        private void WriteFunctionCall(MethodDesc md, MethodEntry me)
        {
            ParameterInfo[] pars = me.Parameters;
            var t = cls.Type;
            var m = me.Info;

            if (!me.IsStatic && !md.IsConstructor)
            {
                WriteCheckSelf();
            }

            for (int n = 0; n < me.ParamNum; n++)
            {
                var p = me.Parameters[n + me.ThisParamNum];
                bool hasParams = p.IsDefined(typeof(ParamArrayAttribute), false);
                CheckArgument(p.ParameterType, n, IsOutArg(p), hasParams, p.HasDefaultValue, p.DefaultValue);
            }

            string ret = "";
            if (me.ReturnType != typeof(void))
            {
                ret = "var ret=";
            }

            if (md.IsConstructor)
            {
                w.Write("var ret = new {0}({1});", cls.CodeName, FuncCallCode(me), ret);
                w.Write("mrb.GetVM().ObjectCache.NewObjectByVal(mrb, _self, ret);");
            }
            else if (me.IsStatic && !me.IsExtension)
            {
                if (m.Name == "op_Multiply")
                    w.Write("{0}a1*a2;", ret);
                else if (m.Name == "op_Subtraction")
                    w.Write("{0}a1-a2;", ret);
                else if (m.Name == "op_Addition")
                    w.Write("{0}a1+a2;", ret);
                else if (m.Name == "op_Division")
                    w.Write("{0}a1/a2;", ret);
                else if (m.Name == "op_UnaryNegation")
                    w.Write("{0}-a1;", ret);
                else if (m.Name == "op_UnaryPlus")
                    w.Write("{0}+a1;", ret);
                else if (m.Name == "op_Equality")
                    w.Write("{0}(a1==a2);", ret);
                else if (m.Name == "op_Inequality")
                    w.Write("{0}(a1!=a2);", ret);
                else if (m.Name == "op_LessThan")
                    w.Write("{0}(a1<a2);", ret);
                else if (m.Name == "op_GreaterThan")
                    w.Write("{0}(a2<a1);", ret);
                else if (m.Name == "op_LessThanOrEqual")
                    w.Write("{0}(a1<=a2);", ret);
                else if (m.Name == "op_GreaterThanOrEqual")
                    w.Write("{0}(a2<=a1);", ret);
                else
                {
                    w.Write("{3}{2}.{0}({1});", MethodDecl(m), FuncCallCode(me), TypeUtil.TypeDecl(t), ret);
                }
            }
            else
            {
                w.Write("{2}self.{0}({1});", MethodDecl(m), FuncCallCode(me), ret);
            }

            if (me.ReturnType != typeof(void) && !md.IsConstructor)
            {
                w.Write("return Converter.make_value(mrb, ret);");
            }
            else
            {
                w.Write("return DLL.mrb_nil_value();");
            }
#if false // TODO: return value with out/ref parameter.
            WriteOk();
            int retcount = 1;
            if (m.ReturnType != typeof(void))
            {

                WritePushValue(m.ReturnType, file);
                retcount = 2;
            }

            // push out/ref value for return value
            if (hasref)
            {
                for (int n = 0; n < pars.Length; n++)
                {
                    ParameterInfo p = pars[n];

                    if (p.ParameterType.IsByRef)
                    {
                        WritePushValue(p.ParameterType, file, string.Format("a{0}", n + 1));
                        retcount++;
                    }
                }
            }

            if (t.IsValueType && m.ReturnType == typeof(void) && !m.IsStatic)
                w.Write( "setBack(mrb,self);");

            w.Write( "return {0};", retcount);
#endif
        }

        bool IsOutArg(ParameterInfo p)
        {
            return (p.IsOut || p.IsDefined(typeof(System.Runtime.InteropServices.OutAttribute), false)) && !p.ParameterType.IsArray;
        }

        public string DefaultValueToCode(object v)
        {
            if (v == null)
            {
                return "null";
            }

            var type = v.GetType();
            if (type == typeof(float) || type == typeof(double)
                || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
            {
                return v.ToString();
            }
            else if (type == typeof(char))
            {
                return $"(char){(int)(char)v}";
            }
            else if (type == typeof(bool))
            {
                return $"{v.ToString().ToLower()}";
            }
            else if (type == typeof(string))
            {
                return $"\"{v}\"";

            }
            else if (type.IsEnum)
            {
                var t = reg.FindByType(type, cls);
                return $"{t.CodeName}.{v}";
            }
            else
            {
                throw new Exception($"Can't support defaultValueType {v}, type = {type}");
            }
        }

        private void CheckArgument(Type t, int n, bool isout, bool isparams, bool hasDefaultValue, object defaultValue)
        {
            w.Write("{0} a{1};", TypeUtil.TypeDecl(t), n);

            if (!isout)
            {
                if (hasDefaultValue)
                {
                    w.Write("if (_argc <= {0}) {{", n);
                    w.Write("a{0} = {1};", n, DefaultValueToCode(defaultValue));
                    w.Write("} else {");
                }

                if (t.IsEnum)
                {
                    w.Write("Converter.checkEnum(mrb, _argv[{0}], out a{0});", n);
                }
                else if (t.BaseType == typeof(System.MulticastDelegate))
                {
                    //tryMake(t);
                    w.Write("Converter.checkDelegate(mrb, _argv[{0}], out a{1});", n, n);
                }
                else if (isparams && false /* TODO */)
                {
                    if (t.GetElementType().IsValueType && !TypeUtil.IsBaseType(t.GetElementType()))
                        w.Write("Converter.checkValueParams(mrb,{0},out a{1});", n, n);
                    else
                        w.Write("Converter.checkParams(mrb,{0},out a{1});", n, n);
                }
                else if (t.IsArray)
                {
                    w.Write("Converter.checkArray(mrb, _argv[{0}],out a{1});", n, n);
                }
                else if (TypeUtil.IsValueType(t))
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        w.Write("Converter.checkNullable(mrb, _argv[{0}], out a{1});", n, n);
                    }
                    else
                    {
                        w.Write("Converter.checkValueType(mrb, _argv[{0}],out a{1});", n, n);
                    }
                }
                else
                {
                    w.Write("Converter.checkType(mrb, _argv[{0}], out a{1});", n, n);
                }

                if (hasDefaultValue)
                {
                    w.Write("}");
                }
            }
        }

        /// <summary>
        /// Create function call code string.
        /// 
        /// FunCallCode() => "a1, a2, a3"
        /// </summary>
        /// <param name="m"></param>
        /// <param name="parOffset"></param>
        /// <returns></returns>
        string FuncCallCode(MethodEntry m)
        {

            string str = "";
            for (int n = 0; n < m.ParamNum; n++)
            {
                ParameterInfo p = m.Parameters[n];
                if (p.ParameterType.IsByRef && p.IsOut)
                {
                    str += string.Format("out a{0}", n);
                }
                else if (p.ParameterType.IsByRef)
                {
                    str += string.Format("ref a{0}", n);
                }
                else
                {
                    str += string.Format("a{0}", n);
                }

                if (n < m.ParamNum - 1)
                {
                    str += ",";
                }
            }
            return str;
        }

        void WriteCheckType(Type t, int n, string v = "v", string nprefix = "")
        {
            if (t.IsEnum)
            {
                w.Write("{0} = ({1})DLL.checkinteger(mrb, {2});", v, TypeUtil.TypeDecl(t), n);
            }
            else if (t.BaseType == typeof(System.MulticastDelegate))
            {
                w.Write("int op=checkDelegate(mrb,{2}{0},out {1});", n, v, nprefix);
            }
            else if (TypeUtil.IsValueType(t))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    w.Write("Converter.checkNullable(mrb,{2}{0},out {1});", n, v, nprefix);
                }
                else
                {
                    w.Write("Converter.checkValueType(mrb,{2}{0},out {1});", n, v, nprefix);
                }
            }
            else if (t.IsArray)
            {
                w.Write("Converter.checkArray(mrb,{2}_argv[{0}],out {1});", n, v, nprefix);
            }
            else
            {
                w.Write("Converter.checkType(mrb,{2}_argv[{0}],out {1});", n, v, nprefix);
            }
        }

        // fill Generic Parameters if needed
        string MethodDecl(MethodBase m)
        {
            if (m.IsGenericMethod)
            {
                string parameters = "";
                bool first = true;
                foreach (Type genericType in m.GetGenericArguments())
                {
                    if (first)
                        first = false;
                    else
                        parameters += ",";
                    parameters += genericType.ToString();

                }
                return string.Format("{0}<{1}>", m.Name, parameters);
            }
            else
                return m.Name;
        }

        #endregion

        #region Utility Writers

        /// <summary>
        /// Write function header.
        /// 
        /// unsafe / _argc+_argv / try .
        /// </summary>
        void WriteFunctionBegin()
        {
            w.Write("unsafe {");
            w.Write("var _argc = DLL.mrb_get_argc(mrb);");
            w.Write("var _argv = DLL.mrb_get_argv(mrb);");
            WriteTry();
        }

        /// <summary>
        /// The pair of WriteFunctionBegin().
        /// </summary>
        void WriteFunctionEnd()
        {
            WriteCatchExecption();
            w.Write("}");
        }

        void WriteTry()
        {
            w.Write("try {");
#if ENABLE_PROFILE
            w.Write( "#if DEBUG");
            w.Write( "var method = System.Reflection.MethodBase.GetCurrentMethod();");
            w.Write( "string methodName = GetMethodName(method);");
            w.Write( "#if UNITY_5_5_OR_NEWER");
            w.Write( "UnityEngine.Profiling.Profiler.BeginSample(methodName);");
            w.Write( "#else");
            w.Write( "Profiler.BeginSample(methodName);");
            w.Write( "#endif");
            w.Write( "#endif");
#endif
        }

        void WriteCatchExecption()
        {
            w.Write("}");
            w.Write("catch(Exception e) {");
            w.Write("DLL.mrb_exc_raise(mrb, Converter.error(mrb, e));");
            w.Write("return default;");
            w.Write("}");
            WriteFinaly();
        }
        void WriteFinaly()
        {
#if ENABLE_PROFILE
            w.Write( "#if DEBUG");
            w.Write( "finally {");
            w.Write( "#if UNITY_5_5_OR_NEWER");
            w.Write( "UnityEngine.Profiling.Profiler.EndSample();");
            w.Write( "#else");
            w.Write( "Profiler.EndSample();");
            w.Write( "#endif");
            w.Write( "}");
            w.Write( "#endif");
#endif
        }

        private void WriteFunctionAttr()
        {
            w.Write("[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]");
#if UNITY_5_3_OR_NEWER
            w.Write( "[UnityEngine.Scripting.Preserve]");
#endif
        }

        void WriteReturn(string ret)
        {
            w.Write("return Converter.make_value(mrb, {0});", ret);
        }

        #endregion

    }
}
