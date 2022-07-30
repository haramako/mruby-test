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
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Runtime.CompilerServices;

    public interface ICustomExportPost { }


#if !SLUA_STANDALONE
    public class LuaCodeGen : MonoBehaviour
#else
    public class LuaCodeGen
#endif
    {
        static public string GenPath = MRubySetting.Instance.UnityEngineGeneratePath;
        static public string WrapperName = "sluaWrapper.dll";
        public delegate void ExportGenericDelegate(Type t, string ns);

#if !SLUA_STANDALONE
        static bool autoRefresh = true;

        static bool IsCompiling
        {
            get
            {
                if (EditorApplication.isCompiling)
                {
                    Debug.Log("Unity Editor is compiling, please wait.");
                }
                return EditorApplication.isCompiling;
            }
        }
#else
        static bool IsCompiling => false;
#endif

#if false
        [InitializeOnLoad]
        public class Startup
        {
            static bool isPlaying = false;
            static Startup()
            {
                EditorApplication.update += Update;
                // use this delegation to ensure dispose luavm at last
                EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
                {
                    switch (state)
                    {
                        case PlayModeStateChange.ExitingEditMode:
                            if (isPlaying == true && EditorApplication.isPlaying == false)
                            {
                                if (LuaState.main != null)
                                    LuaState.main.Dispose();
                                isPlaying = false;
                            }
                            break;
                        case PlayModeStateChange.EnteredPlayMode:
                            isPlaying = true;
                            break;
                    }
                };
            }


            static void Update()
            {
                EditorApplication.update -= Update;
                Lua3rdMeta.Instance.ReBuildTypes();

                // Remind user to generate lua interface code
                var remindGenerate = !EditorPrefs.HasKey("SLUA_REMIND_GENERTE_LUA_INTERFACE") || EditorPrefs.GetBool("SLUA_REMIND_GENERTE_LUA_INTERFACE");
                bool ok = System.IO.Directory.Exists(GenPath + "Unity") || System.IO.File.Exists(GenPath + WrapperName);
                if (!ok && remindGenerate)
                {
                    if (EditorUtility.DisplayDialog("Slua", "Not found lua interface for Unity, generate it now?", "Generate", "No"))
                    {
                        GenerateAll();
                    }
                    else
                    {
                        if (!EditorUtility.DisplayDialog("Slua", "Remind you next time when no lua interface found for Unity?", "OK",
                            "Don't remind me next time!"))
                        {
                            EditorPrefs.SetBool("SLUA_REMIND_GENERTE_LUA_INTERFACE", false);
                        }
                        else
                        {

                            EditorPrefs.SetBool("SLUA_REMIND_GENERTE_LUA_INTERFACE", true);
                        }

                    }
                }
            }

        }
#endif

#if UNITY_2017_2_OR_NEWER
        public static string[] unityModule = new string[] { "UnityEngine","UnityEngine.CoreModule","UnityEngine.UIModule","UnityEngine.TextRenderingModule","UnityEngine.TextRenderingModule",
                "UnityEngine.UnityWebRequestWWWModule","UnityEngine.Physics2DModule","UnityEngine.AnimationModule","UnityEngine.TextRenderingModule","UnityEngine.IMGUIModule","UnityEngine.UnityWebRequestModule",
            "UnityEngine.PhysicsModule", "UnityEngine.UI", "UnityEngine.AudioModule" };
#else
        public static string[] unityModule = null;
#endif

#if false
        [MenuItem("SLua/All/Make")]
        static public void GenerateAll()
        {
            autoRefresh = false;
            GenerateModule(unityModule);
            GenerateUI();
            GenerateAds();
            Custom();
            Generate3rdDll();
            autoRefresh = true;
            AssetDatabase.Refresh();
        }
#endif

#if !SLUA_STANDALONE
        static public bool filterType(Type t, List<string> noUseList, List<string> uselist)
        {
            if (t.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                Debug.Log(t.Name + " is filtered out");
                return false;
            }

            // check type in uselist
            string fullName = t.FullName;
            if (uselist != null && uselist.Count > 0)
            {
                return uselist.Contains(fullName);
            }
            else
            {
                // check type not in nouselist
                foreach (string str in noUseList)
                {
                    if (fullName.Contains(str))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
#if false

        static void GenerateModule(string[] target = null)
        {
#if UNITY_2017_2_OR_NEWER
            if (target != null)
            {
                GenerateFor(target, "Unity/", 0, "BindUnity");
            }
            else
            {
                ModuleSelector wnd = EditorWindow.GetWindow<ModuleSelector>("ModuleSelector");
                wnd.onExport = (string[] module) =>
                {
                    GenerateFor(module, "Unity/", 0, "BindUnity");
                };
            }
#else
            GenerateFor("UnityEngine", "Unity/", 0, "BindUnity");
#endif
        }

#if UNITY_2017_2_OR_NEWER
        [MenuItem("SLua/Unity/Make UnityEngine ...")]
#else
        [MenuItem("SLua/Unity/Make UnityEngine")]
#endif
        static public void Generate()
        {
            GenerateModule();
        }

        [MenuItem("SLua/Unity/Make UnityEngine.UI")]
        static public void GenerateUI()
        {
            GenerateFor("UnityEngine.UI", "Unity/", 1, "BindUnityUI");
        }

        [MenuItem("SLua/Unity/Make UnityEngine.Advertisements")]
        static public void GenerateAds()
        {
            GenerateFor("UnityEngine.Advertisements", "Unity/", 2, "BindUnityAds");
        }
#endif

        static List<Type> GetExportsType(string[] asemblyNames, string genAtPath)
        {

            List<Type> exports = new List<Type>();

            foreach (string asemblyName in asemblyNames)
            {
                Assembly assembly;
                try { assembly = Assembly.Load(asemblyName); }
                catch (Exception) { continue; }

                Type[] types = assembly.GetExportedTypes();

                List<string> uselist;
                List<string> noUseList;

                CustomExport.OnGetNoUseList(out noUseList);
                CustomExport.OnGetUseList(out uselist);

                // Get use and nouse list from custom export.
                object[] aCustomExport = new object[1];
                InvokeEditorMethod<ICustomExportPost>("OnGetUseList", ref aCustomExport);
                if (null != aCustomExport[0])
                {
                    if (null != uselist)
                    {
                        uselist.AddRange((List<string>)aCustomExport[0]);
                    }
                    else
                    {
                        uselist = (List<string>)aCustomExport[0];
                    }
                }

                aCustomExport[0] = null;
                InvokeEditorMethod<ICustomExportPost>("OnGetNoUseList", ref aCustomExport);
                if (null != aCustomExport[0])
                {
                    if ((null != noUseList))
                    {
                        noUseList.AddRange((List<string>)aCustomExport[0]);
                    }
                    else
                    {
                        noUseList = (List<string>)aCustomExport[0];
                    }
                }

                string path = GenPath + genAtPath;
                foreach (Type t in types)
                {
                    if (filterType(t, noUseList, uselist) && Generate(t, path))
                        exports.Add(t);
                }
                Debug.Log("Generate interface finished: " + asemblyName);
            }
            return exports;
        }

        static public void GenerateFor(string[] asemblyNames, string genAtPath, int genOrder, string bindMethod)
        {
            if (IsCompiling)
            {
                return;
            }

            List<Type> exports = GetExportsType(asemblyNames, genAtPath);
            string path = GenPath + genAtPath;
            GenerateBind(exports, bindMethod, genOrder, path);
            if (autoRefresh)
                AssetDatabase.Refresh();
        }

        static public void GenerateFor(string asemblyName, string genAtPath, int genOrder, string bindMethod)
        {
            if (IsCompiling)
            {
                return;
            }


            List<Type> exports = GetExportsType(new string[] { asemblyName }, genAtPath);
            string path = GenPath + genAtPath;
            GenerateBind(exports, bindMethod, genOrder, path);
            if (autoRefresh)
                AssetDatabase.Refresh();

        }

        static String FixPathName(string path)
        {
            if (path.EndsWith("\\") || path.EndsWith("/"))
            {
                return path.Substring(0, path.Length - 1);
            }
            return path;
        }
#if false

        [MenuItem("SLua/Unity/Clear Unity and UI")]
        static public void ClearUnity()
        {
            clear(new string[] { GenPath + "Unity" });
            Debug.Log("Clear Unity & UI complete.");
        }
#endif

        [MenuItem("MRuby/Custom/Make")]
        static public void Custom()
        {
            if (IsCompiling)
            {
                return;
            }

            List<Type> exports = new List<Type>();
            string path = GenPath + "Custom/";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            ExportGenericDelegate fun = (Type t, string ns) =>
            {
                if (Generate(t, ns, path))
                    exports.Add(t);
            };

            HashSet<string> namespaces = CustomExport.OnAddCustomNamespace();

            // Add custom namespaces.
            object[] aCustomExport = null;
            List<object> aCustomNs = LuaCodeGen.InvokeEditorMethod<ICustomExportPost>("OnAddCustomNamespace", ref aCustomExport);
            foreach (object cNsSet in aCustomNs)
            {
                foreach (string strNs in (HashSet<string>)cNsSet)
                {
                    namespaces.Add(strNs);
                }
            }

            Assembly assembly;
            Type[] types;

            try
            {
                // export plugin-dll
                assembly = Assembly.Load("Assembly-CSharp-firstpass");
                types = assembly.GetExportedTypes();
                foreach (Type t in types)
                {
                    if (t.IsDefined(typeof(CustomMRubyClassAttribute), false) || namespaces.Contains(t.Namespace))
                    {
                        fun(t, null);
                    }
                }
            }
            catch (Exception) { }

            // export self-dll
            assembly = Assembly.Load("Assembly-CSharp");
            types = assembly.GetExportedTypes();
            foreach (Type t in types)
            {
                if (t.IsDefined(typeof(CustomMRubyClassAttribute), false) || namespaces.Contains(t.Namespace))
                {
                    fun(t, null);
                }
            }

            CustomExport.OnAddCustomClass(fun);

            //detect interface ICustomExportPost,and call OnAddCustomClass
            aCustomExport = new object[] { fun };
            InvokeEditorMethod<ICustomExportPost>("OnAddCustomClass", ref aCustomExport);

            GenerateBind(exports, "BindCustom", 3, path);
            if (autoRefresh)
                AssetDatabase.Refresh();

            Debug.Log("Generate custom interface finished");
        }

        static public List<object> InvokeEditorMethod<T>(string methodName, ref object[] parameters)
        {
            List<object> aReturn = new List<object>();
            System.Reflection.Assembly editorAssembly = System.Reflection.Assembly.Load("Assembly-CSharp-Editor");
            Type[] editorTypes = editorAssembly.GetExportedTypes();
            foreach (Type t in editorTypes)
            {
                if (typeof(T).IsAssignableFrom(t))
                {
                    System.Reflection.MethodInfo method = t.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (method != null)
                    {
                        object cRes = method.Invoke(null, parameters);
                        if (null != cRes)
                        {
                            aReturn.Add(cRes);
                        }
                    }
                }
            }

            return aReturn;
        }

        static public List<object> GetEditorField<T>(string strFieldName)
        {
            List<object> aReturn = new List<object>();
            System.Reflection.Assembly cEditorAssembly = System.Reflection.Assembly.Load("Assembly-CSharp-Editor");
            Type[] aEditorTypes = cEditorAssembly.GetExportedTypes();
            foreach (Type t in aEditorTypes)
            {
                if (typeof(T).IsAssignableFrom(t))
                {
                    FieldInfo cField = t.GetField(strFieldName, BindingFlags.Static | BindingFlags.Public);
                    if (null != cField)
                    {
                        object cValue = cField.GetValue(t);
                        if (null != cValue)
                        {
                            aReturn.Add(cValue);
                        }
                    }
                }
            }

            return aReturn;
        }

        [MenuItem("SLua/3rdDll/Make")]
        static public void Generate3rdDll()
        {
            if (IsCompiling)
            {
                return;
            }

            List<Type> cust = new List<Type>();
            List<string> assemblyList = new List<string>();
            CustomExport.OnAddCustomAssembly(ref assemblyList);

            //detect interface ICustomExportPost,and call OnAddCustomAssembly
            object[] aCustomExport = new object[] { assemblyList };
            InvokeEditorMethod<ICustomExportPost>("OnAddCustomAssembly", ref aCustomExport);

            foreach (string assemblyItem in assemblyList)
            {
                Assembly assembly = Assembly.Load(assemblyItem);
                Type[] types = assembly.GetExportedTypes();
                foreach (Type t in types)
                {
                    cust.Add(t);
                }
            }
            if (cust.Count > 0)
            {
                List<Type> exports = new List<Type>();
                string path = GenPath + "Dll/";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                foreach (Type t in cust)
                {
                    if (Generate(t, path))
                        exports.Add(t);
                }
                GenerateBind(exports, "BindDll", 2, path);
                if (autoRefresh)
                    AssetDatabase.Refresh();
                Debug.Log("Generate 3rdDll interface finished");
            }
        }
#if false
        [MenuItem("SLua/3rdDll/Clear")]
        static public void Clear3rdDll()
        {
            clear(new string[] { GenPath + "Dll" });
            Debug.Log("Clear AssemblyDll complete.");
        }

#endif
        [MenuItem("MRuby/Custom/Clear")]
        static public void ClearCustom()
        {
            clear(new string[] { GenPath + "Custom" });
            Debug.Log("Clear custom complete.");
        }
#if false

        [MenuItem("SLua/All/Clear")]
        static public void ClearALL()
        {
            clear(new string[] { Path.GetDirectoryName(GenPath) });
            Debug.Log("Clear all complete.");
        }

        [MenuItem("SLua/Compile LuaObject To DLL")]
        static public void CompileDLL()
        {
#region scripts
            List<string> scripts = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Script", new string[1] { Path.GetDirectoryName(GenPath) }).Distinct().ToArray();
            int guidCount = guids.Length;
            for (int i = 0; i < guidCount; i++)
            {
                // path may contains space
                string path = "\"" + AssetDatabase.GUIDToAssetPath(guids[i]) + "\"";
                if (!scripts.Contains(path))
                    scripts.Add(path);
            }

            if (scripts.Count == 0)
            {
                Debug.LogError("No Scripts");
                return;
            }
#endregion

#region libraries
        List<string> libraries = new List<string>();
#if UNITY_2017_2_OR_NEWER
            string[] referenced = unityModule;
#else
            string[] referenced = new string[] { "UnityEngine", "UnityEngine.UI" };
#endif
            string projectPath = Path.GetFullPath(Application.dataPath + "/..").Replace("\\", "/");
            // http://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
            foreach (var assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                UriBuilder uri = new UriBuilder(assem.CodeBase);
                string path = Uri.UnescapeDataString(uri.Path).Replace("\\", "/");
                string name = Path.GetFileNameWithoutExtension(path);
                // ignore dll for Editor
                if ((path.StartsWith(projectPath) && !path.Contains("/Editor/") && !path.Contains("CSharp-Editor"))
                    || referenced.Contains(name))
                {
                    libraries.Add(path);
                }
            }
#endregion

        //generate AssemblyInfo
        string AssemblyInfoFile = Application.dataPath + "/AssemblyInfo.cs";
            File.WriteAllText(AssemblyInfoFile, string.Format("[assembly: UnityEngine.UnityAPICompatibilityVersionAttribute(\"{0}\")]", Application.unityVersion));

#region mono compile            
            string editorData = EditorApplication.applicationContentsPath;
#if UNITY_EDITOR_OSX && !UNITY_5_4_OR_NEWER
			editorData += "/Frameworks";
#endif
            List<string> arg = new List<string>();
            arg.Add("/target:library");
            arg.Add("/sdk:2");
            arg.Add("/w:0");
            arg.Add(string.Format("/out:\"{0}\"", WrapperName));
            arg.Add(string.Format("/r:\"{0}\"", string.Join(";", libraries.ToArray())));
            arg.AddRange(scripts);
            arg.Add(AssemblyInfoFile);

            const string ArgumentFile = "LuaCodeGen.txt";
            File.WriteAllLines(ArgumentFile, arg.ToArray());

            string mcs = editorData + "/MonoBleedingEdge/bin/mcs";
            // wrapping since we may have space
#if UNITY_EDITOR_WIN
            mcs += ".bat";
#endif
#endregion

#region execute bash
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            bool success = false;
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = mcs;
                process.StartInfo.Arguments = "@" + ArgumentFile;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                using (var outputWaitHandle = new System.Threading.AutoResetEvent(false))
                using (var errorWaitHandle = new System.Threading.AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };
                    // http://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    const int timeout = 300;
                    if (process.WaitForExit(timeout * 1000) &&
                        outputWaitHandle.WaitOne(timeout * 1000) &&
                        errorWaitHandle.WaitOne(timeout * 1000))
                    {
                        success = (process.ExitCode == 0);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
#endregion

            Debug.Log(output.ToString());
            if (success)
            {
                Directory.Delete(GenPath, true);
                Directory.CreateDirectory(GenPath);
                File.Move(WrapperName, GenPath + WrapperName);
                // AssetDatabase.Refresh();
                File.Delete(ArgumentFile);
                File.Delete(AssemblyInfoFile);
            }
            else
            {
                Debug.LogError(error.ToString());
            }
        }
#endif

        static void clear(string[] paths)
        {
            try
            {
                foreach (string path in paths)
                {
                    System.IO.Directory.Delete(path, true);
                    AssetDatabase.DeleteAsset(path);
                }
            }
            catch
            {

            }

            AssetDatabase.Refresh();
        }

#endif

#if false
        static void GenerateBind(List<Type> list, string name, int order, string path)
        {
            // delete wrapper dll
            try
            {
                System.IO.File.Delete(GenPath + WrapperName);
            }
            catch { }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            CodeGenerator cg = new CodeGenerator(path);
            cg.path = path;
            //cg.GenerateBind(list, name, order); // TODO: fix
        }
#endif

    }

}
