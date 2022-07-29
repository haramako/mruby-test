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
        static List<string> memberFilter = new List<string>
        {
            "AnimationClip.averageDuration",
            "AnimationClip.averageAngularSpeed",
            "AnimationClip.averageSpeed",
            "AnimationClip.apparentSpeed",
            "AnimationClip.isLooping",
            "AnimationClip.isAnimatorMotion",
            "AnimationClip.isHumanMotion",
            "AnimatorOverrideController.PerformOverrideClipListCleanup",
            "Caching.SetNoBackupFlag",
            "Caching.ResetNoBackupFlag",
            "Light.areaSize",
            "Security.GetChainOfTrustValue",
            "Texture2D.alphaIsTransparency",
            "WWW.movie",
            "WebCamTexture.MarkNonReadable",
            "WebCamTexture.isReadable",
            // i don't know why below 2 functions missed in iOS platform
            "*.OnRebuildRequested",
            // il2cpp not exixts
            "Application.ExternalEval",
            "GameObject.networkView",
            "Component.networkView",
            // unity5
            "AnimatorControllerParameter.name",
            "Input.IsJoystickPreconfigured",
            "Resources.LoadAssetAtPath",
#if UNITY_4_6
			"Motion.ValidateIfRetargetable",
			"Motion.averageDuration",
			"Motion.averageAngularSpeed",
			"Motion.averageSpeed",
			"Motion.apparentSpeed",
			"Motion.isLooping",
			"Motion.isAnimatorMotion",
			"Motion.isHumanMotion",
#endif

			"Light.lightmappingMode",
            "Light.lightmapBakeType",
            "MonoBehaviour.runInEditMode",
            "MonoBehaviour.useGUILayout",
            "PlayableGraph.CreateScriptPlayable",
        };


        static void FilterSpecMethods(out Dictionary<Type, List<MethodInfo>> dic, out Dictionary<Type, Type> overloadedClass)
        {
            dic = new Dictionary<Type, List<MethodInfo>>();
            overloadedClass = new Dictionary<Type, Type>();
            List<string> asems;
            CustomExport.OnGetAssemblyToGenerateExtensionMethod(out asems);

            // Get list from custom export.
            object[] aCustomExport = new object[1];
#if !SLUA_STANDALONE
            LuaCodeGen.InvokeEditorMethod<ICustomExportPost>("OnGetAssemblyToGenerateExtensionMethod", ref aCustomExport);
#endif
            if (null != aCustomExport[0])
            {
                if (null != asems)
                {
                    asems.AddRange((List<string>)aCustomExport[0]);
                }
                else
                {
                    asems = (List<string>)aCustomExport[0];
                }
            }

            foreach (string assstr in asems)
            {
                Assembly assembly = Assembly.Load(assstr);
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (type.IsSealed && !type.IsGenericType && !type.IsNested)
                    {
                        MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
                        foreach (MethodInfo _method in methods)
                        {
                            MethodInfo method = tryFixGenericMethod(_method);
                            if (IsExtensionMethod(method))
                            {
                                Type extendedType = method.GetParameters()[0].ParameterType;
                                if (!dic.ContainsKey(extendedType))
                                {
                                    dic.Add(extendedType, new List<MethodInfo>());
                                }
                                dic[extendedType].Add(method);
                            }
                        }
                    }


                    if (type.IsDefined(typeof(OverloadLuaClassAttribute), false))
                    {
                        OverloadLuaClassAttribute olc = type.GetCustomAttributes(typeof(OverloadLuaClassAttribute), false)[0] as OverloadLuaClassAttribute;
                        if (olc != null)
                        {
                            if (overloadedClass.ContainsKey(olc.targetType))
                                throw new Exception("Can't overload class more than once");
                            overloadedClass.Add(olc.targetType, type);
                        }
                    }
                }
            }
        }

        static bool IsExtensionMethod(MethodBase method)
        {
            return method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);
        }



        static Dictionary<System.Type, List<MethodInfo>> extensionMethods;
        static Dictionary<Type, Type> overloadedClass;

        static CodeGenerator()
        {
            FilterSpecMethods(out extensionMethods, out overloadedClass);
        }

        HashSet<string> funcname = new HashSet<string>();
        Dictionary<string, bool> directfunc = new Dictionary<string, bool>();

        public string givenNamespace;
        public string path;
        public bool includeExtension = MRubySetting.Instance.exportExtensionMethod;
        public EOL eol = MRubySetting.Instance.eol;

        class PropPair
        {
            public string get = "null";
            public string set = "null";
            public bool isInstance = true;
        }
        Dictionary<string, PropPair> propname = new Dictionary<string, PropPair>();

        int indent = 0;

        public string DelegateExportFilename(string path, Type t)
        {
            string f;
            if (t.IsGenericType)
            {
                f = path + string.Format("MRuby{0}_{1}.cs", _Name(GenericBaseName(t)), _Name(GenericName(t)));
            }
            else
            {
                f = path + "MRubyDelegate_" + _Name(t.FullName) + ".cs";
            }
            return f;
        }

        private Registry registry;

        public bool Generate(Type t, Registry _registry)
        {
            registry = _registry;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (!t.IsGenericTypeDefinition && (!IsObsolete(t) && t != typeof(YieldInstruction) && t != typeof(Coroutine))
                || (t.BaseType != null && t.BaseType == typeof(System.MulticastDelegate)))
            {
                if (t.IsNested
                    && ((!t.DeclaringType.IsNested && t.DeclaringType.IsPublic == false)
                    || (t.DeclaringType.IsNested && t.DeclaringType.IsNestedPublic == false)))
                    return false;

                if (t.IsEnum)
                {
                    StreamWriter file = Begin(t);
                    WriteHead(t, file);
                    RegEnumFunction(t, file);
                    End(file);
                }
                else if (t.BaseType == typeof(System.MulticastDelegate))
                {
                    if (t.ContainsGenericParameters)
                        return false;

                    string f = DelegateExportFilename(path, t);

                    StreamWriter file = new StreamWriter(f, false, Encoding.UTF8);
                    file.NewLine = NewLine;
                    WriteDelegate(t, file);
                    file.Close();
                    return false;
                }
                else
                {
                    funcname.Clear();
                    propname.Clear();
                    directfunc.Clear();

                    RegisterNamespaces(registry, t);

                    StreamWriter file = Begin(t);
                    WriteHead(t, file);
                    WriteCSConstructor(t, file);
                    WriteConstructor(t, file);
                    WriteFunction(t, file, false);
                    WriteFunction(t, file, true);
                    WriteField(t, file);
                    RegFunction(t, file);
                    End(file);

                    if (t.BaseType != null && t.BaseType.Name.Contains("UnityEvent`"))
                    {
                        string basename = "LuaUnityEvent_" + _Name(GenericName(t.BaseType)) + ".cs";
                        string f = path + basename;
                        string checkf = LuaCodeGen.GenPath + "Unity/" + basename;
                        if (!File.Exists(checkf)) // if had exported
                        {
                            file = new StreamWriter(f, false, Encoding.UTF8);
                            file.NewLine = NewLine;
                            WriteEvent(t, file);
                            file.Close();
                        }
                    }
                }

                return true;
            }
            return false;
        }

        struct ArgMode
        {
            public int index;
            public int mode;

            public ArgMode(int index, int mode)
            {
                this.index = index;
                this.mode = mode;
            }
        };

        void WriteDelegate(Type t, StreamWriter file)
        {
            string temp = @"
#if false
using System;
using System.Collections.Generic;
namespace MRuby
{
    public partial class LuaDelegation : CSObject
    {

        static internal $RET $FN(LuaFunction ld $ARGS) {
            IntPtr l = ld.L;
            int error = pushTry(l);
";
            MethodInfo mi = t.GetMethod("Invoke");

            temp = temp.Replace("$TN", t.Name);
            temp = temp.Replace("$FN", ExportName(t));

            temp = temp.Replace("$RET",
                mi.ReturnType != typeof(void) ? SimpleType(mi.ReturnType) : "void");

            string args = ArgsList(mi);
            if (args.Length > 0) args = "," + args;
            temp = temp.Replace("$ARGS", args);
            Write(file, temp);

            this.indent = 3;

            ParameterInfo[] pis = mi.GetParameters();

            for (int n = 0; n < pis.Length; n++)
            {
                if (!pis[n].IsOut)
                    Write(file, "Converter.pushValue(l,a{0});", n + 1);
            }

            int outcount = pis.Count((ParameterInfo p) =>
            {
                return p.ParameterType.IsByRef && p.IsOut;
            });

            Write(file, "ld.pcall({0}, error);", pis.Length - outcount);

            int offset = 0;
            if (mi.ReturnType != typeof(void))
            {
                offset = 1;
                WriteValueCheck(file, mi.ReturnType, offset, "ret", "error+");
            }

            for (int n = 0; n < pis.Length; n++)
            {
                if (pis[n].ParameterType.IsByRef)
                {
                    string a = string.Format("a{0}", n + 1);
                    WriteCheckType(file, pis[n].ParameterType, ++offset, a, "error+");
                }
            }


            Write(file, "LuaDLL.lua_settop(l, error-1);");
            if (mi.ReturnType != typeof(void))
                Write(file, "return ret;");

            Write(file, "}");
            Write(file, "}");
            Write(file, "}");
            Write(file, "#endif");
        }

        string ArgsList(MethodInfo m)
        {
            string str = "";
            ParameterInfo[] pars = m.GetParameters();
            for (int n = 0; n < pars.Length; n++)
            {
                string t = SimpleType(pars[n].ParameterType);


                ParameterInfo p = pars[n];
                if (p.ParameterType.IsByRef && p.IsOut)
                    str += string.Format("out {0} a{1}", t, n + 1);
                else if (p.ParameterType.IsByRef)
                    str += string.Format("ref {0} a{1}", t, n + 1);
                else
                    str += string.Format("{0} a{1}", t, n + 1);
                if (n < pars.Length - 1)
                    str += ",";


            }
            return str;
        }

        void tryMake(Type t)
        {

            if (t.BaseType == typeof(System.MulticastDelegate))
            {
                CodeGenerator cg = new CodeGenerator();
                if (File.Exists(cg.DelegateExportFilename(LuaCodeGen.GenPath + "Unity/", t)))
                    return;
                cg.path = this.path;
                cg.Generate(t, null); // TODO: add registry.
            }
        }

        void WriteEvent(Type t, StreamWriter file)
        {
            string temp = @"
#if false
using System;
using System.Collections.Generic;

namespace MRuby.Bind
{
    public class LuaUnityEvent_$CLS : CSObject
    {

        [MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
        static public int AddListener(IntPtr l)
        {
            try
            {
                UnityEngine.Events.UnityEvent<$GN> self = Converter.checkSelf<UnityEngine.Events.UnityEvent<$GN>>(l);
                UnityEngine.Events.UnityAction<$GN> a1;
                checkType(l, 2, out a1);
                self.AddListener(a1);
				pushValue(l,true);
                return 1;
            }
            catch (Exception e)
            {
				return error(l,e);
            }
        }
        [MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
        static public int RemoveListener(IntPtr l)
        {
            try
            {
                UnityEngine.Events.UnityEvent<$GN> self = Converter.checkSelf<UnityEngine.Events.UnityEvent<$GN>>(l);
                UnityEngine.Events.UnityAction<$GN> a1;
                checkType(l, 2, out a1);
                self.RemoveListener(a1);
				pushValue(l,true);
                return 1;
            }
            catch (Exception e)
            {
                return error(l,e);
            }
        }
        [MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
        static public int Invoke(IntPtr l)
        {
            try
            {
                UnityEngine.Events.UnityEvent<$GN> self = Converter.checkSelf<UnityEngine.Events.UnityEvent<$GN>>(l);
" +

                GenericCallDecl(t.BaseType)


+ @"
				pushValue(l,true);
                return 1;
            }
            catch (Exception e)
            {
                return error(l,e);
            }
        }
        static public void RegisterMembers(IntPtr l)
        {
            getTypeTable(l, typeof(LuaUnityEvent_$CLS).FullName);
            addMember(l, AddListener);
            addMember(l, RemoveListener);
            addMember(l, Invoke);
            createTypeMetatable(l, null, typeof(LuaUnityEvent_$CLS), typeof(UnityEngine.Events.UnityEventBase));
        }

        static bool checkType(IntPtr l,int p,out UnityEngine.Events.UnityAction<$GN> ua) {
            LuaDLL.luaL_checktype(l, p, LuaTypes.LUA_TFUNCTION);
            LuaDelegate ld;
            checkType(l, p, out ld);
            if (ld.d != null)
            {
                ua = (UnityEngine.Events.UnityAction<$GN>)ld.d;
                return true;
            }
			l = LuaState.get(l).L;
            ua = ($ARGS) =>
            {
                int error = pushTry(l);
                $PUSHVALUES
                ld.pcall($GENERICCOUNT, error);
                LuaDLL.lua_settop(l,error - 1);
            };
            ld.d = ua;
            return true;
        }
    }
}
#endif";


            temp = temp.Replace("$CLS", _Name(GenericName(t.BaseType)));
            temp = temp.Replace("$FNAME", FullName(t));
            temp = temp.Replace("$GN", GenericName(t.BaseType, ","));
            temp = temp.Replace("$ARGS", ArgsDecl(t.BaseType));
            temp = temp.Replace("$PUSHVALUES", PushValues(t.BaseType));
            temp = temp.Replace("$GENERICCOUNT", t.BaseType.GetGenericArguments().Length.ToString());
            Write(file, temp);
        }

        string GenericCallDecl(Type t)
        {

            try
            {
                Type[] tt = t.GetGenericArguments();
                string ret = "";
                string args = "";
                for (int n = 0; n < tt.Length; n++)
                {
                    string dt = SimpleType(tt[n]);
                    ret += string.Format("				{0} a{1};", dt, n) + NewLine;
                    //ret+=string.Format("				checkType(l,{0},out a{1});",n+2,n)+NewLine;
                    ret += "				" + GetCheckType(tt[n], n + 2, "a" + n) + NewLine;
                    args += ("a" + n);
                    if (n < tt.Length - 1) args += ",";
                }
                ret += string.Format("				self.Invoke({0});", args) + NewLine;
                return ret;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                return "";
            }
        }

        string GetCheckType(Type t, int n, string v = "v", string prefix = "")
        {
            if (t.IsEnum)
            {
                return string.Format("{0} = ({1})LuaDLL.luaL_checkinteger(l, {2});", v, TypeDecl(t), n);
            }
            else if (t.BaseType == typeof(System.MulticastDelegate))
            {
                return string.Format("int op=checkDelegate(l,{2}{0},out {1});", n, v, prefix);
            }
            else if (IsValueType(t))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return string.Format("Converter.checkNullable(l,{2}{0},out {1});", n, v, prefix);
                else
                    return string.Format("Converter.checkValueType(l,{2}{0},out {1});", n, v, prefix);
            }
            else if (t.IsArray)
            {
                return string.Format("Converter.checkArray(l,{2}{0},out {1});", n, v, prefix);
            }
            else
            {
                return string.Format("Converter.checkType(l,{2}{0},out {1});", n, v, prefix);
            }
        }

        string PushValues(Type t)
        {
            try
            {
                Type[] tt = t.GetGenericArguments();
                string ret = "";
                for (int n = 0; n < tt.Length; n++)
                {
                    ret += ("Converter.pushValue(l,v" + n + ");");
                }
                return ret;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                return "";
            }
        }

        string ArgsDecl(Type t)
        {
            try
            {
                Type[] tt = t.GetGenericArguments();
                string ret = "";
                for (int n = 0; n < tt.Length; n++)
                {
                    string dt = SimpleType(tt[n]);
                    dt += (" v" + n);
                    ret += dt;
                    if (n < tt.Length - 1)
                        ret += ",";
                }
                return ret;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                return "";
            }
        }

        void RegEnumFunction(Type t, StreamWriter file)
        {
            // Write export function
            Write(file, "static public void reg(mrb_state l) {");
            Write(file, "getEnumTable(l,\"{0}\");", string.IsNullOrEmpty(givenNamespace) ? FullName(t) : givenNamespace);

            foreach (string name in Enum.GetNames(t))
            {
                Write(file, "addMember(l,{0},\"{1}\");", Convert.ToInt32(Enum.Parse(t, name)), name);
            }

            Write(file, "LuaDLL.lua_pop(l, 1);");
            Write(file, "}");
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
            WriteExtraNamespace(file, t, nsset);
#if UNITY_5_3_OR_NEWER
            Write(file, "[UnityEngine.Scripting.Preserve]");
#endif
            Write(file, "public class {0} {{", ExportName(t));

            Write(file, "static RClass _cls;");
            Write(file, "static mrb_value _cls_value;");
            //Write(file, "readonly {0} obj;", FullName(t));
        }

        // add namespace for extension method
        void WriteExtraNamespace(StreamWriter file, Type t, HashSet<string> nsset)
        {
            List<MethodInfo> lstMI;
            if (extensionMethods.TryGetValue(t, out lstMI))
            {
                foreach (MethodInfo m in lstMI)
                {
                    // if not writed
                    if (!string.IsNullOrEmpty(m.ReflectedType.Namespace) && !nsset.Contains(m.ReflectedType.Namespace))
                    {
                        Write(file, "using {0};", m.ReflectedType.Namespace);
                        nsset.Add(m.ReflectedType.Namespace);
                    }
                }
            }
        }

        private void WriteFunction(Type t, StreamWriter file, bool writeStatic = false)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.DeclaredOnly;
            if (writeStatic)
                bf |= BindingFlags.Static;
            else
                bf |= BindingFlags.Instance;

            MethodInfo[] members = t.GetMethods(bf);
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodInfo mi in members)
                methods.Add(tryFixGenericMethod(mi));

            if (!writeStatic && this.includeExtension)
            {
                if (extensionMethods.ContainsKey(t))
                {
                    methods.AddRange(extensionMethods[t]);
                }
            }
            foreach (MethodInfo mi in methods)
            {
                // �ꎞ�I�� override �𖳌���
                MethodBase[] cons = GetMethods(t, mi.Name, bf);
                if( cons.Length > 1)
                {
                    continue;
                }

                bool instanceFunc;
                if (writeStatic && isPInvoke(mi, out instanceFunc))
                {
                    directfunc.Add(t.FullName + "." + mi.Name, instanceFunc);
                    continue;
                }

                string fn = writeStatic ? staticName(mi.Name) : mi.Name;
                if (mi.MemberType == MemberTypes.Method
                    && !IsObsolete(mi)
                    && !DontExport(mi)
                    && !funcname.Contains(fn)
                    && isUsefullMethod(mi)
                    && !MemberInFilter(t, mi)
                    && !ContainUnsafe(mi))
                {
                    WriteFunctionDec(file, fn);
                    WriteFunctionImpl(file, mi, t, bf);
                    funcname.Add(fn);
                }
            }
        }

        bool isPInvoke(MethodInfo mi, out bool instanceFunc)
        {
            if (mi.IsDefined(typeof(MRuby.MonoPInvokeCallbackAttribute), false))
            {
                instanceFunc = !mi.IsDefined(typeof(StaticExportAttribute), false);
                return true;
            }
            instanceFunc = true;
            return false;
        }

        string staticName(string name)
        {
            if (name.StartsWith("op_"))
                return name;
            return name;
            //return name + "_s";
        }


        bool MemberInFilter(Type t, MemberInfo mi)
        {
            return memberFilter.Contains(t.Name + "." + mi.Name) || memberFilter.Contains("*." + mi.Name);
        }

        bool IsObsolete(MemberInfo t)
        {
            return t.IsDefined(typeof(ObsoleteAttribute), false);
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

        bool trygetOverloadedVersion(Type t, ref string f)
        {
            Type ot;
            if (overloadedClass.TryGetValue(t, out ot))
            {
                MethodInfo mi = ot.GetMethod(f, BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (mi != null && mi.IsDefined(typeof(MRuby.MonoPInvokeCallbackAttribute), false))
                {
                    f = FullName(ot) + "." + f;
                    return true;
                }
            }
            return false;
        }

        public static string RubyMethodName(string name)
        {
            // Special names.
            switch (name)
            {
                case "ToString":
                    return "to_s";
            }

            var sb = new StringBuilder();
            var prevIsUpper = true;
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (prevIsUpper)
                    {
                        sb.Append(char.ToLower(c));
                    }
                    else
                    {
                        sb.Append('_');
                        sb.Append(char.ToLower(c));
                    }
                    prevIsUpper = true;
                }
                else
                {
                    sb.Append(c);
                    prevIsUpper = false;
                }
            }
            return sb.ToString();
        }

        void RegisterNamespaces(Registry r, Type t)
        {
            var curNs = r.RootNamespace;
            var nameList = t.FullName.Split(new char[] { '.', '+' }).SkipLast(1);
            foreach (var name in nameList)
            {
                if (curNs.Children.TryGetValue(name, out var found))
                {
                    curNs = found;
                }
                else
                {
                    curNs = NamespaceInfo.CreateOrGet(curNs, name, null);
                }
            }

            NamespaceInfo.CreateOrGet(curNs, t.Name, t);
        }

        void RegFunction(Type t, StreamWriter file)
        {
#if UNITY_5_3_OR_NEWER
            Write(file, "[UnityEngine.Scripting.Preserve]");
#endif

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

            if (GetValidConstructor(t).Length > 0)
            {
                Write(file, "Converter.define_method(mrb, _cls, \"initialize\", _initialize, DLL.MRB_ARGS_OPT(16));");
            }
            Write(file, "TypeCache.AddType(typeof({0}), Construct);", FullName(t));

            //Write(file, "getTypeTable(l,\"{0}\");", string.IsNullOrEmpty(givenNamespace) ? FullName(t) : givenNamespace);
            foreach (string i in funcname)
            {
                string f = i;
                var isStatic = t.GetMethods().Any(m => m.Name == f && m.IsStatic); // TODO
                trygetOverloadedVersion(t, ref f);
                if (isStatic)
                {
                    Write(file, "Converter.define_class_method(mrb, _cls, \"{0}\", {1}, DLL.MRB_ARGS_OPT(16));", RubyMethodName(f), f);
                }
                else
                {
                    Write(file, "Converter.define_method(mrb, _cls, \"{0}\", {1}, DLL.MRB_ARGS_OPT(16));", RubyMethodName(f), f);
                }
                //Write(file, "addMember(l,{0});", f);
            }
            foreach (string f in directfunc.Keys)
            {
                // TODO
                bool instance = directfunc[f];
                Write(file, "addMember(l,{0},{1});", f, instance ? "true" : "false");
            }

            foreach (string f in propname.Keys)
            {
                PropPair pp = propname[f];
                trygetOverloadedVersion(t, ref pp.get);
                trygetOverloadedVersion(t, ref pp.set);
                Write(file, "Converter.define_property(mrb, _cls,\"{0}\",{1},{2},{3});", RubyMethodName(f), pp.get, pp.set, pp.isInstance ? "true" : "false");
                //Write(file, "addMember(l,\"{0}\",{1},{2},{3});", f, pp.get, pp.set, pp.isInstance ? "true" : "false");
            }
            // TODO
#if false
            if (t.BaseType != null && !CutBase(t.BaseType))
            {
                if (t.BaseType.Name.Contains("UnityEvent`"))
                    Write(file, "createTypeMetatable(l,{2}, typeof({0}),typeof(LuaUnityEvent_{1}));", TypeDecl(t), _Name(GenericName(t.BaseType)), constructorOrNot(t));
                else
                    Write(file, "createTypeMetatable(l,{2}, typeof({0}),typeof({1}));", TypeDecl(t), TypeDecl(t.BaseType), constructorOrNot(t));
            }
            else
                Write(file, "createTypeMetatable(l,{1}, typeof({0}));", TypeDecl(t), constructorOrNot(t));
#endif
            // TODO
            //Write(file, "TypeCache.AddType(typeof(Character), constructor);");
            Write(file, "}");
        }

        string constructorOrNot(Type t)
        {
            ConstructorInfo[] cons = GetValidConstructor(t);
            if (cons.Length > 0 || t.IsValueType)
                return "constructor";
            return "null";
        }

        bool CutBase(Type t)
        {
            if (t.FullName.StartsWith("System.Object"))
                return true;
            return false;
        }

        void WriteSet(StreamWriter file, Type t, string cls, string fn, bool isstatic = false, bool canread = true)
        {
            if (t.BaseType == typeof(MulticastDelegate))
            {
                if (isstatic)
                {
                    Write(file, "if(op==0) {0}.{1}=v;", cls, fn);
                    if (canread)
                    {
                        Write(file, "else if(op==1) {0}.{1}+=v;", cls, fn);
                        Write(file, "else if(op==2) {0}.{1}-=v;", cls, fn);
                    }
                }
                else
                {
                    Write(file, "if(op==0) self.{0}=v;", fn);
                    if (canread)
                    {
                        Write(file, "else if(op==1) self.{0}+=v;", fn);
                        Write(file, "else if(op==2) self.{0}-=v;", fn);
                    }
                }
            }
            else
            {
                if (isstatic)
                {
                    Write(file, "{0}.{1}=v;", cls, fn);
                }
                else
                {
                    Write(file, "self.{0}=v;", fn);
                }
            }
        }

        readonly static string[] keywords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while" };

        static string NormalName(string name)
        {
            if (Array.BinarySearch<string>(keywords, name) >= 0)
            {
                return "@" + name;
            }
            return name;
        }

        private void WriteField(Type t, StreamWriter file)
        {
            // Write field set/get

            FieldInfo[] fields = t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (FieldInfo fi in fields)
            {
                if (DontExport(fi) || IsObsolete(fi))
                    continue;

                PropPair pp = new PropPair();
                pp.isInstance = !fi.IsStatic;

                if (fi.FieldType.BaseType != typeof(MulticastDelegate))
                {
                    WriteFunctionAttr(file);
                    Write(file, "static public mrb_value get_{0}(mrb_state l, mrb_value _self) {{", fi.Name);
                    WriteTry(file);

                    if (fi.IsStatic)
                    {
                        WriteReturn(fi.FieldType, file, string.Format("{0}.{1}", TypeDecl(t), NormalName(fi.Name)));
                    }
                    else
                    {
                        WriteCheckSelf(file, t);
                        WriteReturn(fi.FieldType, file, string.Format("self.{0}", NormalName(fi.Name)));
                    }

                    WriteCatchExecption(file);
                    Write(file, "}");

                    pp.get = "get_" + fi.Name;
                }




                if (!fi.IsLiteral && !fi.IsInitOnly)
                {
                    WriteFunctionAttr(file);
                    Write(file, "static public mrb_value set_{0}(mrb_state l, mrb_value _self) {{", fi.Name);
                    WriteTry(file);
                    if (fi.IsStatic)
                    {
                        Write(file, "{0} v;", TypeDecl(fi.FieldType));
                        WriteCheckType(file, fi.FieldType, 2);
                        WriteSet(file, fi.FieldType, TypeDecl(t), NormalName(fi.Name), true);
                    }
                    else
                    {
                        WriteCheckSelf(file, t);
                        Write(file, "{0} v;", TypeDecl(fi.FieldType));
                        WriteCheckType(file, fi.FieldType, 2);
                        WriteSet(file, fi.FieldType, t.FullName, NormalName(fi.Name));
                    }

                    if (t.IsValueType && !fi.IsStatic)
                        Write(file, "setBack(l,self);");
                    Write(file, "return DLL.mrb_nil_value();");
                    WriteCatchExecption(file);
                    Write(file, "}");

                    pp.set = "set_" + fi.Name;
                }

                propname.Add(fi.Name, pp);
                tryMake(fi.FieldType);
            }
            //for this[]
            List<PropertyInfo> getter = new List<PropertyInfo>();
            List<PropertyInfo> setter = new List<PropertyInfo>();
            // Write property set/get
            PropertyInfo[] props = t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (PropertyInfo fi in props)
            {
                //if (fi.Name == "Item" || IsObsolete(fi) || MemberInFilter(t,fi) || DontExport(fi))
                if (IsObsolete(fi) || MemberInFilter(t, fi) || DontExport(fi))
                    continue;
                if (fi.Name == "Item"
                    || (t.Name == "String" && fi.Name == "Chars")) // for string[]
                {
                    //for this[]
                    if (!fi.GetGetMethod().IsStatic && fi.GetIndexParameters().Length == 1)
                    {
                        if (fi.CanRead && !IsNotSupport(fi.PropertyType))
                            getter.Add(fi);
                        if (fi.CanWrite && fi.GetSetMethod() != null)
                            setter.Add(fi);
                    }
                    continue;
                }
                PropPair pp = new PropPair();
                bool isInstance = true;

                if (fi.CanRead && fi.GetGetMethod() != null)
                {
                    if (!IsNotSupport(fi.PropertyType))
                    {
                        WriteFunctionAttr(file);
                        Write(file, "static public mrb_value get_{0}(mrb_state l, mrb_value _self) {{", fi.Name);
                        WriteTry(file);

                        if (fi.GetGetMethod().IsStatic)
                        {
                            isInstance = false;
                            WriteReturn(fi.PropertyType, file, string.Format("{0}.{1}", TypeDecl(t), NormalName(fi.Name)));
                        }
                        else
                        {
                            WriteCheckSelf(file, t);
                            WriteReturn(fi.PropertyType, file, string.Format("self.{0}", NormalName(fi.Name)));
                        }

                        WriteCatchExecption(file);
                        Write(file, "}");
                        pp.get = "get_" + fi.Name;
                    }

                }

                if (fi.CanWrite && fi.GetSetMethod() != null)
                {
                    WriteFunctionAttr(file);
                    Write(file, "static public mrb_value set_{0}(mrb_state l, mrb_value _self) {{", fi.Name);
                    WriteTry(file);
                    if (fi.GetSetMethod().IsStatic)
                    {
                        WriteValueCheck(file, fi.PropertyType, 2);
                        WriteSet(file, fi.PropertyType, TypeDecl(t), NormalName(fi.Name), true, fi.CanRead);
                        isInstance = false;
                    }
                    else
                    {
                        WriteCheckSelf(file, t);
                        WriteValueCheck(file, fi.PropertyType, 2);
                        WriteSet(file, fi.PropertyType, TypeDecl(t), NormalName(fi.Name), false, fi.CanRead);
                    }

                    if (t.IsValueType)
                        Write(file, "setBack(l,self);");
                    Write(file, "return DLL.mrb_nil_value();");
                    WriteCatchExecption(file);
                    Write(file, "}");
                    pp.set = "set_" + fi.Name;
                }
                pp.isInstance = isInstance;

                propname.Add(fi.Name, pp);
                tryMake(fi.PropertyType);
            }
            //for this[]
            WriteItemFunc(t, file, getter, setter);
        }
        void WriteItemFunc(Type t, StreamWriter file, List<PropertyInfo> getter, List<PropertyInfo> setter)
        {

            //Write property this[] set/get
            if (getter.Count > 0)
            {
                //get
                bool first_get = true;
                WriteFunctionAttr(file);
                Write(file, "static public int getItem(IntPtr l) {");
                WriteTry(file);
                WriteCheckSelf(file, t);
                if (getter.Count == 1)
                {
                    PropertyInfo _get = getter[0];
                    ParameterInfo[] infos = _get.GetIndexParameters();
                    WriteValueCheck(file, infos[0].ParameterType, 2, "v");
                    Write(file, "var ret = self[v];");
                    WriteReturn(_get.PropertyType, file, "ret");
                }
                else
                {
                    Write(file, "LuaTypes t = LuaDLL.lua_type(l, 2);");
                    for (int i = 0; i < getter.Count; i++)
                    {
                        PropertyInfo fii = getter[i];
                        ParameterInfo[] infos = fii.GetIndexParameters();
                        Write(file, "{0}(Converter.matchType(l,2,t,typeof({1}))){{", first_get ? "if" : "else if", infos[0].ParameterType);
                        WriteValueCheck(file, infos[0].ParameterType, 2, "v");
                        Write(file, "var ret = self[v];");
                        WriteReturn(fii.PropertyType, file, "ret");
                        Write(file, "}");
                        first_get = false;
                    }
                    WriteNotMatch(file, "getItem");
                }
                WriteCatchExecption(file);
                Write(file, "}");
                funcname.Add("getItem");
            }
            if (setter.Count > 0)
            {
                bool first_set = true;
                WriteFunctionAttr(file);
                Write(file, "static public int setItem(IntPtr l) {");
                WriteTry(file);
                WriteCheckSelf(file, t);
                if (setter.Count == 1)
                {
                    PropertyInfo _set = setter[0];
                    ParameterInfo[] infos = _set.GetIndexParameters();
                    WriteValueCheck(file, infos[0].ParameterType, 2);
                    WriteValueCheck(file, _set.PropertyType, 3, "c");
                    Write(file, "self[v]=c;");
                    Write(file, "return DLL.mrb_nil_value()1;");
                }
                else
                {
                    Write(file, "LuaTypes t = LuaDLL.lua_type(l, 2);");
                    for (int i = 0; i < setter.Count; i++)
                    {
                        PropertyInfo fii = setter[i];
                        if (t.BaseType != typeof(MulticastDelegate))
                        {
                            ParameterInfo[] infos = fii.GetIndexParameters();
                            Write(file, "{0}(Converter.matchType(l,2,t,typeof({1}))){{", first_set ? "if" : "else if", infos[0].ParameterType);
                            WriteValueCheck(file, infos[0].ParameterType, 2, "v");
                            WriteValueCheck(file, fii.PropertyType, 3, "c");
                            Write(file, "self[v]=c;");
                            Write(file, "return DLL.mrb_nil_value()1;");
                            Write(file, "}");
                            first_set = false;
                        }
                        if (t.IsValueType)
                            Write(file, "setBack(l,self);");
                    }
                    WriteNotMatch(file, "setItem");
                }
                WriteCatchExecption(file);
                Write(file, "}");
                funcname.Add("setItem");
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

        void WriteValueCheck(StreamWriter file, Type t, int n, string v = "v", string nprefix = "")
        {
            Write(file, "{0} {1};", SimpleType(t), v);
            WriteCheckType(file, t, n, v, nprefix);
        }

        private void WriteFunctionAttr(StreamWriter file)
        {
            Write(file, "[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]");
#if UNITY_5_3_OR_NEWER
            Write(file, "[UnityEngine.Scripting.Preserve]");
#endif
        }

        ConstructorInfo[] GetValidConstructor(Type t)
        {
            List<ConstructorInfo> ret = new List<ConstructorInfo>();
            if (t.GetConstructor(Type.EmptyTypes) == null && t.IsAbstract && t.IsSealed)
                return ret.ToArray();
            if (t.IsAbstract)
                return ret.ToArray();
            if (t.BaseType != null && t.BaseType.Name == "MonoBehaviour")
                return ret.ToArray();

            ConstructorInfo[] cons = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            foreach (ConstructorInfo ci in cons)
            {
                if (!IsObsolete(ci) && !DontExport(ci) && !ContainUnsafe(ci))
                    ret.Add(ci);
            }
            return ret.ToArray();
        }

        bool ContainUnsafe(MethodBase mi)
        {
            foreach (ParameterInfo p in mi.GetParameters())
            {
                if (p.ParameterType.FullName.Contains("*"))
                    return true;
            }
            return false;
        }

        bool DontExport(MemberInfo mi)
        {
            var methodString = string.Format("{0}.{1}", mi.DeclaringType, mi.Name);
            if (CustomExport.FunctionFilterList.Contains(methodString))
                return true;
            // directly ignore any components .ctor
            if (mi.DeclaringType.IsSubclassOf(typeof(UnityEngine.Component)))
            {
                if (mi.MemberType == MemberTypes.Constructor)
                {
                    return true;
                }
            }

            // Check in custom export function filter list.
#if !SLUA_STANDALONE
            List<object> aFuncFilterList = LuaCodeGen.GetEditorField<ICustomExportPost>("FunctionFilterList");
            foreach (object aFilterList in aFuncFilterList)
            {
                if (((List<string>)aFilterList).Contains(methodString))
                {
                    return true;
                }
            }
#endif

            if (mi.DeclaringType.IsGenericType && mi.DeclaringType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                if (mi.MemberType == MemberTypes.Constructor)
                {
                    ConstructorInfo constructorInfo = mi as ConstructorInfo;
                    var parameterInfos = constructorInfo.GetParameters();
                    if (parameterInfos.Length > 0)
                    {
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(parameterInfos[0].ParameterType))
                        {
                            return true;
                        }
                    }
                }
                else if (mi.MemberType == MemberTypes.Method)
                {
                    var methodInfo = mi as MethodInfo;
                    if (methodInfo.Name == "TryAdd" || methodInfo.Name == "Remove" && methodInfo.GetParameters().Length == 2)
                    {
                        return true;
                    }
                }
            }

            return mi.IsDefined(typeof(DoNotToLuaAttribute), false);
        }


        private void WriteCSConstructor(Type t, StreamWriter file)
        {
            Write(file, "static CSObject Construct(mrb_state mrb, object obj) => ObjectCache.NewObject(mrb, _cls_value, obj);", ExportName(t));
        }

        private void WriteConstructor(Type t, StreamWriter file)
        {
            ConstructorInfo[] cons = GetValidConstructor(t);
            if (cons.Length > 0)
            {
                WriteFunctionAttr(file);
                Write(file, "static public mrb_value _initialize(mrb_state l, mrb_value _self) {");
                WriteTry(file);
                if (cons.Length > 1)
                    Write(file, "int argc = LuaDLL.lua_gettop(l);");
                Write(file, "{0} o;", TypeDecl(t));
                bool first = true;
                for (int n = 0; n < cons.Length; n++)
                {
                    ConstructorInfo ci = cons[n];
                    ParameterInfo[] pars = ci.GetParameters();

                    if (cons.Length > 1)
                    {
                        if (isUniqueArgsCount(cons, ci))
                            Write(file, "{0}(argc=={1}){{", first ? "if" : "else if", ci.GetParameters().Length + 1);
                        else
                            Write(file, "{0}(Converter.matchType(l,argc,2{1})){{", first ? "if" : "else if", TypeDecl(pars));
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
                    if (cons.Length == 1)
                        WriteCatchExecption(file);
                    Write(file, "}");
                    first = false;
                }

                if (cons.Length > 1)
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

        bool IsNotSupport(Type t)
        {
            if (t.IsSubclassOf(typeof(Delegate)))
                return true;
            return false;
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

        // try filling generic parameters
        static MethodInfo tryFixGenericMethod(MethodInfo method)
        {
            if (!method.ContainsGenericParameters)
                return method;

            try
            {
                Type[] genericTypes = method.GetGenericArguments();
                for (int j = 0; j < genericTypes.Length; j++)
                {
                    Type[] contraints = genericTypes[j].GetGenericParameterConstraints();
                    if (contraints != null && contraints.Length == 1 && contraints[0] != typeof(ValueType))
                        genericTypes[j] = contraints[0];
                    else
                        return method;
                }
                // only fixed here
                return method.MakeGenericMethod(genericTypes);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return method;
        }

        bool isUsefullMethod(MethodInfo method)
        {
            if (method.Name != "GetType" && method.Name != "GetHashCode" && method.Name != "Equals" &&
                /* method.Name != "ToString" && */ method.Name != "Clone" &&
                method.Name != "GetEnumerator" && method.Name != "CopyTo" &&
                method.Name != "op_Implicit" && method.Name != "op_Explicit" &&
                !method.Name.StartsWith("get_", StringComparison.Ordinal) &&
                !method.Name.StartsWith("set_", StringComparison.Ordinal) &&
                !method.Name.StartsWith("add_", StringComparison.Ordinal) &&
                !IsObsolete(method) && !method.ContainsGenericParameters &&
                method.ToString() != "Int32 Clamp(Int32, Int32, Int32)" &&
                !method.Name.StartsWith("remove_", StringComparison.Ordinal))
            {
                return true;
            }
            return false;
        }

        void WriteFunctionDec(StreamWriter file, string name)
        {
            WriteFunctionAttr(file);
            Write(file, "static public mrb_value {0}(mrb_state l, mrb_value _self) {{", name);

        }

        MethodBase[] GetMethods(Type t, string name, BindingFlags bf)
        {

            List<MethodBase> methods = new List<MethodBase>();

            if (this.includeExtension && ((bf & BindingFlags.Instance) == BindingFlags.Instance))
            {
                if (extensionMethods.ContainsKey(t))
                {
                    foreach (MethodInfo m in extensionMethods[t])
                    {
                        if (m.Name == name
                           && !IsObsolete(m)
                           && !DontExport(m)
                           && isUsefullMethod(m))
                        {
                            methods.Add(m);
                        }
                    }
                }
            }

            MemberInfo[] cons = t.GetMember(name, bf);
            foreach (MemberInfo _m in cons)
            {
                MemberInfo m = _m;
                if (m.MemberType == MemberTypes.Method) m = tryFixGenericMethod((MethodInfo)m);
                if (m.MemberType == MemberTypes.Method
                    && !IsObsolete(m)
                    && !DontExport(m)
                    && isUsefullMethod((MethodInfo)m))
                    methods.Add((MethodBase)m);
            }
            methods.Sort((a, b) =>
           {
               return a.GetParameters().Length - b.GetParameters().Length;
           });
            return methods.ToArray();

        }

        void WriteNotMatch(StreamWriter file, string fn)
        {
            WriteError(file, string.Format("No matched override function {0} to call", fn));
        }

        void WriteFunctionImpl(StreamWriter file, MethodInfo m, Type t, BindingFlags bf)
        {
            WriteTry(file);
            MethodBase[] cons = GetMethods(t, m.Name, bf);

            Dictionary<string, MethodInfo> overridedMethods = null;

            if (cons.Length == 1) // no override function
            {
                if (isUsefullMethod(m) && !m.ReturnType.ContainsGenericParameters && !m.ContainsGenericParameters) // don't support generic method
                    WriteFunctionCall(m, file, t, bf);
                else
                {
                    WriteNotMatch(file, m.Name);
                }
            }
            else // 2 or more override function
            {
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
            }
            WriteCatchExecption(file);
            Write(file, "}");

            WriteOverridedMethod(file, overridedMethods, t, bf);
        }

        void WriteOverridedMethod(StreamWriter file, Dictionary<string, MethodInfo> methods, Type t, BindingFlags bf)
        {
            if (methods == null)
                return;

            foreach (var pair in methods)
            {
                string fn = pair.Value.IsStatic ? staticName(pair.Key) : pair.Key;
                WriteSimpleFunction(file, fn, pair.Value, t, bf);
                funcname.Add(fn);
            }

        }

        void WriteSimpleFunction(StreamWriter file, string fn, MethodInfo mi, Type t, BindingFlags bf)
        {
            WriteFunctionDec(file, fn);
            WriteTry(file);
            WriteFunctionCall(mi, file, t, bf);
            WriteCatchExecption(file);
            Write(file, "}");
        }

        int GetMethodArgc(MethodBase mi)
        {
            bool isExtension = IsExtensionMethod(mi);
            if (isExtension)
                return mi.GetParameters().Length - 1;
            return mi.GetParameters().Length;
        }

        bool isUniqueArgsCount(MethodBase[] cons, MethodBase mi)
        {
            int argcLength = GetMethodArgc(mi);
            foreach (MethodBase member in cons)
            {
                MethodBase m = (MethodBase)member;
                if (m == mi)
                    continue;
                if (argcLength == GetMethodArgc(m))
                    return false;
            }
            return true;
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
                Write(file, "{0} self=({0})Converter.checkSelf(l, _self);", TypeDecl(t));
        }


        private void WriteFunctionCall(MethodInfo m, StreamWriter file, Type t, BindingFlags bf)
        {

            bool isExtension = IsExtensionMethod(m) && (bf & BindingFlags.Instance) == BindingFlags.Instance;
            ParameterInfo[] pars = m.GetParameters();

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

        void WritePushValue(Type t, StreamWriter file)
        {
            if (t.IsEnum)
                Write(file, "Converter.pushEnum(l,(int)ret);");
            else if (t.IsInterface && t.IsDefined(typeof(CustomMRubyClassAttribute), false))
                Write(file, "Converter.pushInterface(l,ret, typeof({0}));", TypeDecl(t));
            else
                Write(file, "Converter.pushValue(l,ret);");
        }

        void WriteReturn(Type t, StreamWriter file, string ret)
        {
            Write(file, "return Converter.make_value(l, {0});", ret);
        }

        void WritePushValue(Type t, StreamWriter file, string ret)
        {
            if (t.IsEnum)
                Write(file, "Converter.pushEnum(l,(int){0});", ret);
            else if (t.IsInterface && t.IsDefined(typeof(CustomMRubyClassAttribute), false))
                Write(file, "Converter.pushInterface(l,{0}, typeof({1}));", ret, TypeDecl(t));
            else
                Write(file, "Converter.pushValue(l,{0});", ret);
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
                    tryMake(t);
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
