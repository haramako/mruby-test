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
        public string path;
        public bool includeExtension = MRubySetting.Instance.exportExtensionMethod;
        public EOL eol = MRubySetting.Instance.eol;

        int indent = 0;

        private Registry registry;

        public void GenerateClass(ClassDesc cls)
        {
            var t = cls.Type;
            StreamWriter file = Begin(t);
            WriteHead(t, file);
            WriteCSConstructor(t, file);
            WriteConstructor(cls, file);
            WriteFunction(cls, file, false);
            //WriteFunction(cls, file, true);
            WriteField(cls, file);
            RegFunction(cls, file);
            End(file);
        }

        StreamWriter Begin(Type t)
        {
            string clsname = ExportName(t);
            string f = path + clsname + ".cs";
            StreamWriter file = new StreamWriter(f, false, Encoding.UTF8);
            file.NewLine = NewLine;
            return file;
        }

        private void End(StreamWriter file)
        {
            Write(file, "}");
            Write(file, "#endif");
            file.Flush();
            file.Close();
        }

        private void WriteHead(Type t, StreamWriter file)
        {
            HashSet<string> nsset = new HashSet<string>();
            Write(file, "#if true");
            Write(file, "using System;");
            Write(file, "using MRuby;");
            Write(file, "using System.Collections.Generic;");
            nsset.Add("System");
            nsset.Add("SLua");
            nsset.Add("System.Collections.Generic");
            //WriteExtraNamespace(file, t, nsset);
#if UNITY_5_3_OR_NEWER
            Write(file, "[UnityEngine.Scripting.Preserve]");
#endif
            Write(file, "public class {0} {{", ExportName(t));

            Write(file, "static RClass _cls;");
            Write(file, "static mrb_value _cls_value;");
            //Write(file, "readonly {0} obj;", FullName(t));
        }

        private void WriteFunction(ClassDesc cls, StreamWriter file, bool writeStatic = false)
        {
            var t = cls.Type;

            foreach (var m in cls.MethodDescs.Values)
            {
                // ˆêŽž“I‚É override ‚ð–³Œø‰»
                if (m.Methods.Count > 1)
                {
                    continue;
                }

                if (m.IsGeneric)
                {
                    continue;
                }

                // TODO
                if (m.Name.StartsWith("get_") || m.Name.StartsWith("set_"))
                {
                    continue;
                }

                WriteFunctionDec(file, m);
                WriteFunctionImpl(file, cls, m);
            }
        }

        string NewLine
        {
            get
            {
                switch (eol)
                {
                    case EOL.Native:
                        return System.Environment.NewLine;
                    case EOL.CRLF:
                        return "\r\n";
                    case EOL.CR:
                        return "\r";
                    case EOL.LF:
                        return "\n";
                    default:
                        return "";
                }
            }
        }

        void RegFunction(ClassDesc cls, StreamWriter file)
        {
#if UNITY_5_3_OR_NEWER
            Write(file, "[UnityEngine.Scripting.Preserve]");
#endif
            var t = cls.Type;

            var fullname = string.IsNullOrEmpty(givenNamespace) ? FullName(t) : givenNamespace;
            var fullnames = fullname.Split('.');

            // Write export function
            Write(file, "static public void RegisterMembers(mrb_state mrb) {");

            if (t.BaseType != null && t.BaseType.Name.Contains("UnityEvent`"))
            {
                Write(file, "LuaUnityEvent_{1}.reg(l);", FullName(t), _Name((GenericName(t.BaseType))));
            }

            Write(file, "_cls = Converter.GetClass(mrb, \"{0}\");", FullName(t).Replace(".", "::"));
            Write(file, "_cls_value = DLL.mrb_obj_value(_cls.val);");

            if (cls.Constructors.Count > 0)
            {
                Write(file, "Converter.define_method(mrb, _cls, \"initialize\", _initialize, DLL.MRB_ARGS_OPT(16));");
            }
            Write(file, "TypeCache.AddType(typeof({0}), Construct);", FullName(t));

            foreach (var md in cls.MethodDescs.Values)
            {
                var f = md.Name;
                if (md.IsGeneric)
                {
                    continue;
                }
                // TODO
                if (md.Methods.Count > 1)
                {
                    continue;
                }

                if (md.IsStatic)
                {
                    Write(file, "Converter.define_class_method(mrb, _cls, \"{0}\", {1}, DLL.MRB_ARGS_OPT(16));", md.RubyName, f); // TODO
                }
                else
                {
                    Write(file, "Converter.define_method(mrb, _cls, \"{0}\", {1}, DLL.MRB_ARGS_OPT(16));", md.RubyName, f);
                }
            }

            foreach (var f in cls.Fields.Values)
            {
                if (f.CanRead)
                {
                    Write(file, "Converter.define_method(mrb, _cls, \"{0}\", {1}, DLL.MRB_ARGS_OPT(16));", f.RubyName, f.GetterName);
                }
                if (f.CanWrite)
                {
                    Write(file, "Converter.define_method(mrb, _cls, \"{0}=\", {1}, DLL.MRB_ARGS_OPT(16));", f.RubyName, f.SetterName);
                }
            }

            Write(file, "}");
        }

        private void WriteField(ClassDesc cls, StreamWriter file)
        {
            var t = cls.Type;

            foreach (FieldDesc f in cls.Fields.Values)
            {
                // TODO
                //if (DontExport(fi) || IsObsolete(fi))
                //  continue;

                if (f.Type.BaseType != typeof(MulticastDelegate))
                {
                    WriteFunctionAttr(file);
                    Write(file, "static public mrb_value get_{0}(mrb_state l, mrb_value _self) {{", f.Name);
                    WriteTry(file);

                    if (f.IsStatic)
                    {
                        WriteReturn(f.Type, file, string.Format("{0}.{1}", TypeDecl(t), f.Name));
                    }
                    else
                    {
                        WriteCheckSelf(file, t);
                        WriteReturn(f.Type, file, string.Format("self.{0}", f.Name));
                    }

                    WriteCatchExecption(file);
                    Write(file, "}");

                }

                if (f.CanWrite)
                {
                    WriteFunctionAttr(file);
                    Write(file, "static public mrb_value set_{0}(mrb_state l, mrb_value _self) {{", f.Name);
                    WriteTry(file);
                    if (f.IsStatic)
                    {
                        Write(file, "{0} v;", TypeDecl(f.Type));
                        WriteCheckType(file, f.Type, 2);
                    }
                    else
                    {
                        WriteCheckSelf(file, t);
                        Write(file, "{0} v;", TypeDecl(f.Type));
                        WriteCheckType(file, f.Type, 2);
                    }

                    if (t.IsValueType && !f.IsStatic)
                        Write(file, "setBack(l,self);");
                    Write(file, "return DLL.mrb_nil_value();");
                    WriteCatchExecption(file);
                    Write(file, "}");
                }
            }
        }

        void WriteTry(StreamWriter file)
        {
            Write(file, "try {");
#if ENABLE_PROFILE
            Write(file, "#if DEBUG");
            Write(file, "var method = System.Reflection.MethodBase.GetCurrentMethod();");
            Write(file, "string methodName = GetMethodName(method);");
            Write(file, "#if UNITY_5_5_OR_NEWER");
            Write(file, "UnityEngine.Profiling.Profiler.BeginSample(methodName);");
            Write(file, "#else");
            Write(file, "Profiler.BeginSample(methodName);");
            Write(file, "#endif");
            Write(file, "#endif");
#endif
        }

        void WriteCatchExecption(StreamWriter file)
        {
            Write(file, "}");
            Write(file, "catch(Exception e) {");
            Write(file, "DLL.mrb_exc_raise(l, Converter.error(l, e));");
            Write(file, "return default;");
            Write(file, "}");
            WriteFinaly(file);
        }
        void WriteFinaly(StreamWriter file)
        {
#if ENABLE_PROFILE
            Write(file, "#if DEBUG");
            Write(file, "finally {");
            Write(file, "#if UNITY_5_5_OR_NEWER");
            Write(file, "UnityEngine.Profiling.Profiler.EndSample();");
            Write(file, "#else");
            Write(file, "Profiler.EndSample();");
            Write(file, "#endif");
            Write(file, "}");
            Write(file, "#endif");
#endif
        }

        void WriteCheckType(StreamWriter file, Type t, int n, string v = "v", string nprefix = "")
        {
            if (t.IsEnum)
                Write(file, "{0} = ({1})LuaDLL.luaL_checkinteger(l, {2});", v, TypeDecl(t), n);
            else if (t.BaseType == typeof(System.MulticastDelegate))
                Write(file, "int op=checkDelegate(l,{2}{0},out {1});", n, v, nprefix);
            else if (IsValueType(t))
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                    Write(file, "Converter.checkNullable(l,{2}{0},out {1});", n, v, nprefix);
                else
                    Write(file, "Converter.checkValueType(l,{2}{0},out {1});", n, v, nprefix);
            else if (t.IsArray)
                Write(file, "Converter.checkArray(l,{2}{0},out {1});", n, v, nprefix);
            else
                Write(file, "Converter.checkType(l,{2}{0},out {1});", n, v, nprefix);
        }

        private void WriteFunctionAttr(StreamWriter file)
        {
            Write(file, "[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]");
#if UNITY_5_3_OR_NEWER
            Write(file, "[UnityEngine.Scripting.Preserve]");
#endif
        }

        private void WriteCSConstructor(Type t, StreamWriter file)
        {
            Write(file, "static CSObject Construct(mrb_state mrb, object obj) => ObjectCache.NewObject(mrb, _cls_value, obj);", ExportName(t));
        }

        private void WriteConstructor(ClassDesc cls, StreamWriter file)
        {
            var t = cls.Type;
            var cons = cls.Constructors;
            if (cons.Count > 0)
            {
                WriteFunctionAttr(file);
                Write(file, "static public mrb_value _initialize(mrb_state l, mrb_value _self) {");
                WriteTry(file);
                if (cons.Count > 1)
                    Write(file, "int argc = LuaDLL.lua_gettop(l);");
                Write(file, "{0} o;", TypeDecl(t));
                bool first = true;
                for (int n = 0; n < cons.Count; n++)
                {
                    ConstructorInfo ci = cons[n];
                    ParameterInfo[] pars = ci.GetParameters();

                    if (cons.Count > 1)
                    {
#if false
                        if (isUniqueArgsCount(cons, ci))
                            Write(file, "{0}(argc=={1}){{", first ? "if" : "else if", ci.GetParameters().Length + 1);
                        else
                            Write(file, "{0}(Converter.matchType(l,argc,2{1})){{", first ? "if" : "else if", TypeDecl(pars));
#endif
                        throw new Exception("not implemented");
                    }

                    for (int k = 0; k < pars.Length; k++)
                    {
                        ParameterInfo p = pars[k];
                        bool hasParams = p.IsDefined(typeof(ParamArrayAttribute), false);
                        CheckArgument(file, p.ParameterType, k, 2, IsOutArg(p), hasParams, p.HasDefaultValue, p.DefaultValue);
                    }
                    Write(file, "o=new {0}({1});", TypeDecl(t), FuncCall(ci));
                    Write(file, "ObjectCache.NewObjectByVal(l, _self, o);");
                    Write(file, "return DLL.mrb_nil_value();");
#if false
                    if (t.Name == "String") // if export system.string, push string as ud not lua string
                        WriteReturn(file, "o");
                    else
                        WriteReturn(file, "o");
#endif
                    if (cons.Count == 1)
                        WriteCatchExecption(file);
                    Write(file, "}");
                    first = false;
                }

                if (cons.Count > 1)
                {
                    if (t.IsValueType)
                    {
                        Write(file, "{0}(argc=={1}){{", first ? "if" : "else if", 0);
                        Write(file, "o=new {0}();", FullName(t));
                        Write(file, "return Converter.make_value(l, o);");
                        Write(file, "}");
                    }

                    Write(file, "return Converter.error(l,\"New object failed.\");");
                    WriteCatchExecption(file);
                    Write(file, "}");
                }
            }
            else if (t.IsValueType) // default constructor
            {
                WriteFunctionAttr(file);
                Write(file, "static public mrb_value _initialize(mrb_state l) {");
                WriteTry(file);
                Write(file, "{0} o;", FullName(t));
                Write(file, "o=new {0}();", FullName(t));
                WriteReturn(file, "o");
                WriteCatchExecption(file);
                Write(file, "}");
            }
        }

        void WriteOk(StreamWriter file)
        {
            Write(file, "Converter.pushValue(l,true);");
        }
        void WriteBad(StreamWriter file)
        {
            Write(file, "Converter.pushValue(l,false);");
        }

        void WriteError(StreamWriter file, string err)
        {
            WriteBad(file);
            Write(file, "LuaDLL.lua_pushstring(l,\"{0}\");", err);
            Write(file, "return 2;");
        }

        void WriteReturn(StreamWriter file, string val)
        {
            Write(file, "return Converter.make_value(l, o);");
        }

        string[] prefix = new string[] { "System.Collections.Generic" };
        string RemoveRef(string s, bool removearray = true)
        {
            if (s.EndsWith("&")) s = s.Substring(0, s.Length - 1);
            if (s.EndsWith("[]") && removearray) s = s.Substring(0, s.Length - 2);
            if (s.StartsWith(prefix[0])) s = s.Substring(prefix[0].Length + 1, s.Length - prefix[0].Length - 1);

            s = s.Replace("+", ".");
            if (s.Contains("`"))
            {
                string regstr = @"`\d";
                Regex r = new Regex(regstr, RegexOptions.None);
                s = r.Replace(s, "");
                s = s.Replace("[", "<");
                s = s.Replace("]", ">");
            }
            return s;
        }

        string GenericBaseName(Type t)
        {
            string n = t.FullName;
            if (n.IndexOf('[') > 0)
            {
                n = n.Substring(0, n.IndexOf('['));
            }
            return n.Replace("+", ".");
        }

        string GenericName(Type t, string sep = "_")
        {
            try
            {
                Type[] tt = t.GetGenericArguments();
                string ret = "";
                for (int n = 0; n < tt.Length; n++)
                {
                    string dt = SimpleType(tt[n]);
                    ret += dt;
                    if (n < tt.Length - 1)
                        ret += sep;
                }
                return ret;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                return "";
            }
        }

        string _Name(string n)
        {
            string ret = "";
            for (int i = 0; i < n.Length; i++)
            {
                if (char.IsLetterOrDigit(n[i]))
                    ret += n[i];
                else
                    ret += "_";
            }
            return ret;
        }

        string TypeDecl(ParameterInfo[] pars, int paraOffset = 0)
        {
            string ret = "";
            for (int n = paraOffset; n < pars.Length; n++)
            {
                ret += ",typeof(";
                if (pars[n].IsOut)
                    ret += "LuaOut";
                else
                    ret += SimpleType(pars[n].ParameterType);
                ret += ")";
            }
            return ret;
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

        void WriteFunctionDec(StreamWriter file, MethodDesc m)
        {
            WriteFunctionAttr(file);
            Write(file, "static public mrb_value {0}(mrb_state l, mrb_value _self) {{", m.Name);

        }

        void WriteFunctionImpl(StreamWriter file, ClassDesc cls, MethodDesc md)
        {
            WriteTry(file);
            if (md.Methods.Count == 1) // no override function
            {
                WriteFunctionCall(file, cls, md);
            }
            else // 2 or more override function
            {
#if false
                Write(file, "int argc = LuaDLL.lua_gettop(l);");

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
                                Write(file, "{0}(argc=={1}){{", first ? "if" : "else if", mi.IsStatic ? mi.GetParameters().Length : mi.GetParameters().Length + 1);
                            else
                                Write(file, "{0}(Converter.matchType(l,argc,{1}{2})){{", first ? "if" : "else if", mi.IsStatic && !isExtension ? 1 : 2, TypeDecl(pars, isExtension ? 1 : 0));
                            WriteFunctionCall(mi, file, t, bf);
                            Write(file, "}");
                            first = false;
                        }
                    }
                }
                WriteNotMatch(file, m.Name);
#endif
            }
            WriteCatchExecption(file);
            Write(file, "}");

            //riteOverridedMethod(file, overridedMethods, t, bf); // TODO
        }

        void WriteSimpleFunction(StreamWriter file, string fn, MethodInfo mi, Type t, BindingFlags bf)
        {
            //WriteFunctionDec(file, fn);
            WriteTry(file);
            //WriteFunctionCall(mi, file, t, bf);
            WriteCatchExecption(file);
            Write(file, "}");
        }

        void WriteCheckSelf(StreamWriter file, Type t)
        {
            if (t.IsValueType)
            {
                Write(file, "{0} self;", TypeDecl(t));
                if (IsBaseType(t))
                    Write(file, "Converter.checkType(l,1,out self);");
                else
                    Write(file, "Converter.checkValueType(l,1,out self);");
            }
            else
            {
                Write(file, "{0} self=({0})Converter.checkSelf(l, _self);", TypeDecl(t));
            }
        }

        private void WriteFunctionCall(StreamWriter file, ClassDesc cls, MethodDesc md)
        {
            // bool isExtension = IsExtensionMethod(m) && (bf & BindingFlags.Instance) == BindingFlags.Instance;
            bool isExtension = false; // TODO
            var m = md.Methods[0];
            ParameterInfo[] pars = m.GetParameters();
            var t = cls.Type;

            // Is argument number more than parameter number?
            var requireParameterNum = m.GetParameters().ToArray().TakeWhile(p => !p.HasDefaultValue).Count();
            Write(file, "var _argc = DLL.mrb_get_argc(l);");
            Write(file, "if (_argc > {0}){{", m.GetParameters().Length);
            Write(file, "  throw new Exception($\"wrong number of arguments (given {{_argc}}, expected {0})\");", m.GetParameters().Length);
            Write(file, "}");
            Write(file, "else if (_argc < {0}){{", requireParameterNum);
            Write(file, "  throw new Exception($\"wrong number of arguments (given {{_argc}}, expected {0})\");", requireParameterNum);
            Write(file, "}");

            int argIndex = 1;
            int parOffset = 0;
            if (!m.IsStatic)
            {
                WriteCheckSelf(file, t);
                argIndex++;
            }
            else if (isExtension)
            {
                WriteCheckSelf(file, t);
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
                CheckArgument(file, p.ParameterType, n, argIndex, IsOutArg(p), hasParams, p.HasDefaultValue, p.DefaultValue);
            }

            string ret = "";
            if (m.ReturnType != typeof(void))
            {
                ret = "var ret=";
            }

            if (m.IsStatic && !isExtension)
            {
                if (m.Name == "op_Multiply")
                    Write(file, "{0}a1*a2;", ret);
                else if (m.Name == "op_Subtraction")
                    Write(file, "{0}a1-a2;", ret);
                else if (m.Name == "op_Addition")
                    Write(file, "{0}a1+a2;", ret);
                else if (m.Name == "op_Division")
                    Write(file, "{0}a1/a2;", ret);
                else if (m.Name == "op_UnaryNegation")
                    Write(file, "{0}-a1;", ret);
                else if (m.Name == "op_UnaryPlus")
                    Write(file, "{0}+a1;", ret);
                else if (m.Name == "op_Equality")
                    Write(file, "{0}(a1==a2);", ret);
                else if (m.Name == "op_Inequality")
                    Write(file, "{0}(a1!=a2);", ret);
                else if (m.Name == "op_LessThan")
                    Write(file, "{0}(a1<a2);", ret);
                else if (m.Name == "op_GreaterThan")
                    Write(file, "{0}(a2<a1);", ret);
                else if (m.Name == "op_LessThanOrEqual")
                    Write(file, "{0}(a1<=a2);", ret);
                else if (m.Name == "op_GreaterThanOrEqual")
                    Write(file, "{0}(a2<=a1);", ret);
                else
                {
                    Write(file, "{3}{2}.{0}({1});", MethodDecl(m), FuncCall(m), TypeDecl(t), ret);
                }
            }
            else
            {
                Write(file, "{2}self.{0}({1});", MethodDecl(m), FuncCall(m, parOffset), ret);
            }

            if (m.ReturnType != typeof(void))
            {
                Write(file, "return Converter.make_value(l, ret);");
            }
            else
            {
                Write(file, "return DLL.mrb_nil_value();");
            }
#if false // TODO: return value with out/ref parameter.
            WriteOk(file);
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
                Write(file, "setBack(l,self);");

            Write(file, "return {0};", retcount);
#endif
        }

        string SimpleType(Type t)
        {

            string tn = t.Name;
            switch (tn)
            {
                case "Single":
                    return "float";
                case "String":
                    return "string";
                case "Double":
                    return "double";
                case "Boolean":
                    return "bool";
                case "Int32":
                    return "int";
                case "Object":
                    return FullName(t);
                default:
                    tn = TypeDecl(t);
                    tn = tn.Replace("System.Collections.Generic.", "");
                    tn = tn.Replace("System.Object", "object");
                    return tn;
            }
        }

        void WriteReturn(Type t, StreamWriter file, string ret)
        {
            Write(file, "return Converter.make_value(l, {0});", ret);
        }

        void Write(StreamWriter file, string fmt, params object[] args)
        {

            fmt = System.Text.RegularExpressions.Regex.Replace(fmt, @"\r\n?|\n|\r", NewLine);

            if (fmt.StartsWith("}")) indent--;

            for (int n = 0; n < indent; n++)
                file.Write("\t");


            if (args.Length == 0)
                file.WriteLine(fmt);
            else
            {
                string line = string.Format(fmt, args);
                file.WriteLine(line);
            }

            if (fmt.EndsWith("{")) indent++;
        }

        bool IsOutArg(ParameterInfo p)
        {
            return (p.IsOut || p.IsDefined(typeof(System.Runtime.InteropServices.OutAttribute), false)) && !p.ParameterType.IsArray;
        }

        public string DefaultValueToString(object v)
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

        private void CheckArgument(StreamWriter file, Type t, int n, int argstart, bool isout, bool isparams, bool hasDefaultValue, object defaultValue)
        {
            Write(file, "{0} a{1};", TypeDecl(t), n + 1);

            if (!isout)
            {
                if (hasDefaultValue)
                {
                    Write(file, "if (_argc < {0}) {{", n + 1);
                    Write(file, "    a{0} = {1};", n + 1, DefaultValueToString(defaultValue));
                    Write(file, "} else {");
                }

                if (t.IsEnum)
                    Write(file, "a{0} = ({1})LuaDLL.luaL_checkinteger(l, {2});", n + 1, TypeDecl(t), n + argstart);
                else if (t.BaseType == typeof(System.MulticastDelegate))
                {
                    //tryMake(t);
                    Write(file, "Converter.checkDelegate(l,{0},out a{1});", n + argstart, n + 1);
                }
                else if (isparams)
                {
                    if (t.GetElementType().IsValueType && !IsBaseType(t.GetElementType()))
                        Write(file, "Converter.checkValueParams(l,{0},out a{1});", n + argstart, n + 1);
                    else
                        Write(file, "Converter.checkParams(l,{0},out a{1});", n + argstart, n + 1);
                }
                else if (t.IsArray)
                    Write(file, "Converter.checkArray(l,{0},out a{1});", n, n + 1);
                else if (IsValueType(t))
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                        Write(file, "Converter.checkNullable(l,{0},out a{1});", n + argstart, n + 1);
                    else
                        Write(file, "Converter.checkValueType(l,{0},out a{1});", n + argstart, n + 1);
                }
                else
                {
                    Write(file, "Converter.checkType(l,{0},out a{1});", n /* + argstart */, n + 1);
                }

                if (hasDefaultValue)
                {
                    Write(file, "}");
                }
            }
        }

        bool IsValueType(Type t)
        {
            if (t.IsByRef) t = t.GetElementType();
            return t.BaseType == typeof(ValueType) && !IsBaseType(t);
        }

        bool IsBaseType(Type t)
        {
            return t.IsPrimitive || CSObject.isImplByLua(t);
        }

        string FullName(string str)
        {
            if (str == null)
            {
                throw new NullReferenceException();
            }
            return RemoveRef(str.Replace("+", "."));
        }

        string TypeDecl(Type t)
        {
            if (t.IsGenericType)
            {
                string ret = GenericBaseName(t);

                string gs = "";
                gs += "<";
                Type[] types = t.GetGenericArguments();
                for (int n = 0; n < types.Length; n++)
                {
                    gs += TypeDecl(types[n]);
                    if (n < types.Length - 1)
                        gs += ",";
                }
                gs += ">";

                ret = Regex.Replace(ret, @"`\d", gs);

                return ret;
            }
            if (t.IsArray)
            {
                return TypeDecl(t.GetElementType()) + "[]";
            }
            else
                return RemoveRef(t.ToString(), false);
        }

        string ExportName(Type t)
        {
            if (t.IsGenericType)
            {
                return string.Format("MRuby_{0}_{1}", _Name(GenericBaseName(t)), _Name(GenericName(t)));
            }
            else
            {
                string name = RemoveRef(t.FullName, true);
                name = "MRuby_" + name;
                return name.Replace(".", "_");
            }
        }

        string FullName(Type t)
        {
            if (t.FullName == null)
            {
                return t.Name;
            }
            return FullName(t.FullName);
        }

        string FuncCall(MethodBase m, int parOffset = 0)
        {

            string str = "";
            ParameterInfo[] pars = m.GetParameters();
            for (int n = parOffset; n < pars.Length; n++)
            {
                ParameterInfo p = pars[n];
                if (p.ParameterType.IsByRef && p.IsOut)
                    str += string.Format("out a{0}", n + 1);
                else if (p.ParameterType.IsByRef)
                    str += string.Format("ref a{0}", n + 1);
                else
                    str += string.Format("a{0}", n + 1);
                if (n < pars.Length - 1)
                    str += ",";
            }
            return str;
        }
    }
}
