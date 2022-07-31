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
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Runtime.CompilerServices;

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
            w = new CodeWriter(path + cls.ExportName() + ".cs");
        }

        public void Generate()
        {
            var t = cls.Type;
            WriteHead();
            WriteCSConstructor();
            WriteConstructor();
            WriteFunction(false);
            //WriteFunction(cls, true);
            WriteField();
            RegFunction();
            End();
        }

        private void End()
        {
            w.Write("}");
            w.Write("#endif");
            w.Dispose();
        }

        private void WriteHead()
        {
            w.Write("#if true");
            w.Write("using System;");
            w.Write("using MRuby;");
            w.Write("using System.Collections.Generic;");
            w.Write("public class {0} {{", cls.ExportName());

            w.Write("static RClass _cls;");
            w.Write("static mrb_value _cls_value;");
        }

        private void WriteFunction(bool writeStatic = false)
        {
            var t = cls.Type;

            foreach (var m in cls.MethodDescs.Values)
            {
                // ˆêŽž“I‚É override ‚ð–³Œø‰»
                if (m.IsOverloaded)
                {
                    continue;
                }

                if (m.IsGeneric)
                {
                    continue;
                }

                WriteFunctionDec(m);
                WriteFunctionImpl(cls, m);
            }
        }

        void RegFunction()
        {
#if UNITY_5_3_OR_NEWER
            w.Write( "[UnityEngine.Scripting.Preserve]");
#endif
            var t = cls.Type;

            var fullname = string.IsNullOrEmpty(givenNamespace) ? FullName(t) : givenNamespace;
            var fullnames = fullname.Split('.');

            // Write export function
            w.Write("static public void RegisterMembers(mrb_state mrb) {");

            if (t.BaseType != null && t.BaseType.Name.Contains("UnityEvent`"))
            {
                w.Write("LuaUnityEvent_{1}.reg(l);", FullName(t), reg.FindByType(cls.BaseType).RubyFullName);
            }

            w.Write("_cls = Converter.GetClass(mrb, \"{0}\");", FullName(t).Replace(".", "::"));
            w.Write("_cls_value = DLL.mrb_obj_value(_cls.val);");

            if (cls.Constructors.Count > 0)
            {
                w.Write("Converter.define_method(mrb, _cls, \"initialize\", _initialize, DLL.MRB_ARGS_OPT(16));");
            }
            w.Write("TypeCache.AddType(typeof({0}), Construct);", FullName(t));

            foreach (var md in cls.MethodDescs.Values)
            {
                var f = md.Name;
                if (md.IsGeneric)
                {
                    continue;
                }
                // TODO
                if (md.IsOverloaded)
                {
                    continue;
                }

                if (md.IsStatic)
                {
                    w.Write("Converter.define_class_method(mrb, _cls, \"{0}\", {1}, DLL.MRB_ARGS_OPT(16));", md.RubyName, f); // TODO
                }
                else
                {
                    w.Write("Converter.define_method(mrb, _cls, \"{0}\", {1}, DLL.MRB_ARGS_OPT(16));", md.RubyName, f);
                }
            }

            foreach (var f in cls.Fields.Values)
            {
                if (f.CanRead)
                {
                    w.Write("Converter.define_method(mrb, _cls, \"{0}\", {1}, DLL.MRB_ARGS_OPT(16));", f.RubyName, f.GetterName);
                }
                if (f.CanWrite)
                {
                    w.Write("Converter.define_method(mrb, _cls, \"{0}=\", {1}, DLL.MRB_ARGS_OPT(16));", f.RubyName, f.SetterName);
                }
            }

            w.Write("}");
        }

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
                    w.Write("static public mrb_value get_{0}(mrb_state l, mrb_value _self) {{", f.Name);
                    WriteTry();

                    if (f.IsStatic)
                    {
                        WriteReturn(string.Format("{0}.{1}", TypeCond.TypeDecl(t), f.Name));
                    }
                    else
                    {
                        WriteCheckSelf();
                        WriteReturn(string.Format("self.{0}", f.Name));
                    }

                    WriteCatchExecption();
                    w.Write("}");

                }

                if (f.CanWrite)
                {
                    WriteFunctionAttr();
                    w.Write("static public mrb_value set_{0}(mrb_state l, mrb_value _self) {{", f.Name);
                    WriteTry();
                    if (f.IsStatic)
                    {
                        w.Write("{0} v;", TypeCond.TypeDecl(f.Type));
                        WriteCheckType(f.Type, 2);
                    }
                    else
                    {
                        WriteCheckSelf();
                        w.Write("{0} v;", TypeCond.TypeDecl(f.Type));
                        WriteCheckType(f.Type, 2);
                    }

                    if (t.IsValueType && !f.IsStatic)
                        w.Write("setBack(l,self);");
                    w.Write("return DLL.mrb_nil_value();");
                    WriteCatchExecption();
                    w.Write("}");
                }
            }
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
            w.Write("DLL.mrb_exc_raise(l, Converter.error(l, e));");
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

        void WriteCheckType(Type t, int n, string v = "v", string nprefix = "")
        {
            if (t.IsEnum)
            {
                w.Write("{0} = ({1})LuaDLL.luaL_checkinteger(l, {2});", v, TypeCond.TypeDecl(t), n);
            }
            else if (t.BaseType == typeof(System.MulticastDelegate))
            {
                w.Write("int op=checkDelegate(l,{2}{0},out {1});", n, v, nprefix);
            }
            else if (TypeCond.IsValueType(t))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    w.Write("Converter.checkNullable(l,{2}{0},out {1});", n, v, nprefix);
                }
                else
                {
                    w.Write("Converter.checkValueType(l,{2}{0},out {1});", n, v, nprefix);
                }
            }
            else if (t.IsArray)
            {
                w.Write("Converter.checkArray(l,{2}{0},out {1});", n, v, nprefix);
            }
            else
            {
                w.Write("Converter.checkType(l,{2}{0},out {1});", n, v, nprefix);
            }
        }

        private void WriteFunctionAttr()
        {
            w.Write("[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]");
#if UNITY_5_3_OR_NEWER
            w.Write( "[UnityEngine.Scripting.Preserve]");
#endif
        }

        private void WriteCSConstructor()
        {
            w.Write("static CSObject Construct(mrb_state mrb, object obj) => ObjectCache.NewObject(mrb, _cls_value, obj);", cls.ExportName());
        }

        private void WriteConstructor()
        {
            var t = cls.Type;
            var cons = cls.Constructors;
            if (cons.Count > 0)
            {
                WriteFunctionAttr();
                w.Write("static public mrb_value _initialize(mrb_state l, mrb_value _self) {");
                WriteTry();
                if (cons.Count > 1)
                    w.Write("int argc = LuaDLL.lua_gettop(l);");
                w.Write("{0} o;", TypeCond.TypeDecl(t));
                bool first = true;
                for (int n = 0; n < cons.Count; n++)
                {
                    ConstructorInfo ci = cons[n];
                    ParameterInfo[] pars = ci.GetParameters();

                    if (cons.Count > 1)
                    {
#if false
                        if (isUniqueArgsCount(cons, ci))
                            w.Write( "{0}(argc=={1}){{", first ? "if" : "else if", ci.GetParameters().Length + 1);
                        else
                            w.Write( "{0}(Converter.matchType(l,argc,2{1})){{", first ? "if" : "else if", TypeDecl(pars));
#endif
                        throw new Exception("not implemented");
                    }

                    for (int k = 0; k < pars.Length; k++)
                    {
                        ParameterInfo p = pars[k];
                        bool hasParams = p.IsDefined(typeof(ParamArrayAttribute), false);
                        CheckArgument(p.ParameterType, k, 2, IsOutArg(p), hasParams, p.HasDefaultValue, p.DefaultValue);
                    }
                    w.Write("o=new {0}({1});", TypeCond.TypeDecl(t), FuncCallCode(ci));
                    w.Write("ObjectCache.NewObjectByVal(l, _self, o);");
                    w.Write("return DLL.mrb_nil_value();");
#if false
                    if (t.Name == "String") // if export system.string, push string as ud not lua string
                        WriteReturn(file, "o");
                    else
                        WriteReturn(file, "o");
#endif
                    if (cons.Count == 1)
                        WriteCatchExecption();
                    w.Write("}");
                    first = false;
                }

                if (cons.Count > 1)
                {
                    if (t.IsValueType)
                    {
                        w.Write("{0}(argc=={1}){{", first ? "if" : "else if", 0);
                        w.Write("o=new {0}();", FullName(t));
                        w.Write("return Converter.make_value(l, o);");
                        w.Write("}");
                    }

                    w.Write("return Converter.error(l,\"New object failed.\");");
                    WriteCatchExecption();
                    w.Write("}");
                }
            }
            else if (t.IsValueType) // default constructor
            {
                WriteFunctionAttr();
                w.Write("static public mrb_value _initialize(mrb_state l) {");
                WriteTry();
                w.Write("{0} o;", FullName(t));
                w.Write("o=new {0}();", FullName(t));
                WriteReturn("o");
                WriteCatchExecption();
                w.Write("}");
            }
        }

        void WriteReturn(string ret)
        {
            w.Write("return Converter.make_value(l, {0});", ret);
        }

        // fill Generic Parameters if needed
        string MethodDecl(MethodInfo m)
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

        void WriteFunctionDec(MethodDesc m)
        {
            WriteFunctionAttr();
            w.Write("static public mrb_value {0}(mrb_state l, mrb_value _self) {{", m.Name);

        }

        void WriteFunctionImpl(ClassDesc cls, MethodDesc md)
        {
            WriteTry();
            if (!md.IsOverloaded) // no override function
            {
                WriteFunctionCall(cls, md);
            }
            else // 2 or more override function
            {
#if false
                w.Write( "int argc = LuaDLL.lua_gettop(l);");

                bool first = true;
                for (int n = 0; n < cons.Length; n++)
                {
                    if (cons[n].MemberType == MemberTypes.Method)
                    {
                        MethodInfo mi = cons[n] as MethodInfo;
                        if (ContainUnsafe(mi))
                        {
                            continue;
                        }
                        if (mi.IsDefined(typeof(LuaOverrideAttribute), false))
                        {
                            if (overridedMethods == null)
                                overridedMethods = new Dictionary<string, MethodInfo>();

                            LuaOverrideAttribute attr = mi.GetCustomAttributes(typeof(LuaOverrideAttribute), false)[0] as LuaOverrideAttribute;
                            string fn = attr.fn;
                            if (overridedMethods.ContainsKey(fn))
                                throw new Exception(string.Format("Found function with same name {0}", fn));
                            overridedMethods.Add(fn, mi);
                            continue;
                        }

                        ParameterInfo[] pars = mi.GetParameters();
                        if (isUsefullMethod(mi)
                            && !mi.ReturnType.ContainsGenericParameters
                            /*&& !ContainGeneric(pars)*/) // don't support generic method
                        {
                            bool isExtension = IsExtensionMethod(mi) && (bf & BindingFlags.Instance) == BindingFlags.Instance;
                            if (isUniqueArgsCount(cons, mi))
                                w.Write( "{0}(argc=={1}){{", first ? "if" : "else if", mi.IsStatic ? mi.GetParameters().Length : mi.GetParameters().Length + 1);
                            else
                                w.Write( "{0}(Converter.matchType(l,argc,{1}{2})){{", first ? "if" : "else if", mi.IsStatic && !isExtension ? 1 : 2, TypeDecl(pars, isExtension ? 1 : 0));
                            WriteFunctionCall(mi, t, bf);
                            w.Write( "}");
                            first = false;
                        }
                    }
                }
                WriteNotMatch(file, m.Name);
#endif
            }
            WriteCatchExecption();
            w.Write("}");

            //riteOverridedMethod(file, overridedMethods, t, bf); // TODO
        }

        void WriteSimpleFunction(string fn, MethodInfo mi, BindingFlags bf)
        {
            //WriteFunctionDec(file, fn);
            WriteTry();
            //WriteFunctionCall(mi, file, t, bf);
            WriteCatchExecption();
            w.Write("}");
        }

        void WriteCheckSelf()
        {
            var t = cls.Type;
            if (t.IsValueType)
            {
                w.Write("{0} self;", TypeCond.TypeDecl(t));
                if (TypeCond.IsBaseType(t))
                    w.Write("Converter.checkType(l,1,out self);");
                else
                    w.Write("Converter.checkValueType(l,1,out self);");
            }
            else
            {
                w.Write("{0} self=({0})Converter.checkSelf(l, _self);", TypeCond.TypeDecl(t));
            }
        }

        private void WriteFunctionCall(ClassDesc cls, MethodDesc md)
        {
            // bool isExtension = IsExtensionMethod(m) && (bf & BindingFlags.Instance) == BindingFlags.Instance;
            bool isExtension = false; // TODO
            var m = md.Methods[0];
            ParameterInfo[] pars = m.GetParameters();
            var t = cls.Type;

            // Is argument number more than parameter number?
            var requireParameterNum = m.GetParameters().ToArray().TakeWhile(p => !p.HasDefaultValue).Count();
            w.Write("var _argc = DLL.mrb_get_argc(l);");
            w.Write("if (_argc > {0}){{", m.GetParameters().Length);
            w.Write("  throw new Exception($\"wrong number of arguments (given {{_argc}}, expected {0})\");", m.GetParameters().Length);
            w.Write("}");
            w.Write("else if (_argc < {0}){{", requireParameterNum);
            w.Write("  throw new Exception($\"wrong number of arguments (given {{_argc}}, expected {0})\");", requireParameterNum);
            w.Write("}");

            int argIndex = 1;
            int parOffset = 0;
            if (!m.IsStatic)
            {
                WriteCheckSelf();
                argIndex++;
            }
            else if (isExtension)
            {
                WriteCheckSelf();
                parOffset++;
            }
            for (int n = parOffset; n < pars.Length; n++)
            {
                ParameterInfo p = pars[n];
                string pn = p.ParameterType.Name;
                if (pn.EndsWith("&"))
                {
                }

                bool hasParams = p.IsDefined(typeof(ParamArrayAttribute), false);
                CheckArgument(p.ParameterType, n, argIndex, IsOutArg(p), hasParams, p.HasDefaultValue, p.DefaultValue);
            }

            string ret = "";
            if (m.ReturnType != typeof(void))
            {
                ret = "var ret=";
            }

            if (m.IsStatic && !isExtension)
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
                    w.Write("{3}{2}.{0}({1});", MethodDecl(m), FuncCallCode(m), TypeCond.TypeDecl(t), ret);
                }
            }
            else
            {
                w.Write("{2}self.{0}({1});", MethodDecl(m), FuncCallCode(m, parOffset), ret);
            }

            if (m.ReturnType != typeof(void))
            {
                w.Write("return Converter.make_value(l, ret);");
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
                w.Write( "setBack(l,self);");

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
            if (type == typeof(int) || type == typeof(bool) || type == typeof(float))
            {
                return v.ToString();
            }
            else if (type == typeof(string))
            {
                return $"\"{v}\"";

            }
            else
            {
                throw new Exception($"Can't support defaultValueType {v}, type = {type}");
            }
        }

        private void CheckArgument(Type t, int n, int argstart, bool isout, bool isparams, bool hasDefaultValue, object defaultValue)
        {
            w.Write("{0} a{1};", TypeCond.TypeDecl(t), n);

            if (!isout)
            {
                if (hasDefaultValue)
                {
                    w.Write("if (_argc < {0}) {{", n);
                    w.Write("    a{0} = {1};", n, DefaultValueToCode(defaultValue));
                    w.Write("} else {");
                }

                if (t.IsEnum)
                    w.Write("a{0} = ({1})LuaDLL.luaL_checkinteger(l, {2});", n, TypeCond.TypeDecl(t), n + argstart);
                else if (t.BaseType == typeof(System.MulticastDelegate))
                {
                    //tryMake(t);
                    w.Write("Converter.checkDelegate(l,{0},out a{1});", n + argstart, n);
                }
                else if (isparams)
                {
                    if (t.GetElementType().IsValueType && !TypeCond.IsBaseType(t.GetElementType()))
                        w.Write("Converter.checkValueParams(l,{0},out a{1});", n + argstart, n);
                    else
                        w.Write("Converter.checkParams(l,{0},out a{1});", n + argstart, n);
                }
                else if (t.IsArray)
                    w.Write("Converter.checkArray(l,{0},out a{1});", n, n);
                else if (TypeCond.IsValueType(t))
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                        w.Write("Converter.checkNullable(l,{0},out a{1});", n + argstart, n);
                    else
                        w.Write("Converter.checkValueType(l,{0},out a{1});", n + argstart, n);
                }
                else
                {
                    w.Write("Converter.checkType(l,{0},out a{1});", n /* + argstart */, n);
                }

                if (hasDefaultValue)
                {
                    w.Write("}");
                }
            }
        }

        string FullName(Type t)
        {
            if (t.FullName == null)
            {
                return t.Name;
            }
            return t.FullName.Replace("+", ".");
        }

        /// <summary>
        /// Create function call code string.
        /// 
        /// FunCallCode() => "a1, a2, a3"
        /// </summary>
        /// <param name="m"></param>
        /// <param name="parOffset"></param>
        /// <returns></returns>
        string FuncCallCode(MethodBase m, int parOffset = 0)
        {

            string str = "";
            ParameterInfo[] pars = m.GetParameters();
            for (int n = parOffset; n < pars.Length; n++)
            {
                ParameterInfo p = pars[n];
                if (p.ParameterType.IsByRef && p.IsOut)
                    str += string.Format("out a{0}", n);
                else if (p.ParameterType.IsByRef)
                    str += string.Format("ref a{0}", n);
                else
                    str += string.Format("a{0}", n);
                if (n < pars.Length - 1)
                    str += ",";
            }
            return str;
        }
    }
}
