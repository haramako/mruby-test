using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace MRuby.CodeGen
{
    public static class Logger
    {
        public static void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }

    public class CodeGenUtil
    {
        public CodeGenUtil()
        {

        }

        public static bool Generate(Registry reg, Type t, string ns, string path)
        {
            CodeGenerator cg = new CodeGenerator();
            cg.givenNamespace = ns;
            cg.path = path;
            var ok = cg.Generate(t, reg);

            return ok;
        }

        public static void GenerateBind(Registry reg, string path)
        {
            var nsGen = new NamespaceGen(reg, path);
            nsGen.Generate();
        }

        public static List<Type> GetMRubyClasses(string[] asemblyNames)
        {
            List<Type> exports = new List<Type>();

            exports.Add(typeof(System.Object));

            foreach (string asemblyName in asemblyNames)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.Load(asemblyName);
                }
                catch (Exception)
                {
                    continue;
                }

                Type[] types = assembly.GetExportedTypes();

                foreach (Type t in types)
                {
                    var attr = (CustomMRubyClassAttribute)Attribute.GetCustomAttribute(t, typeof(CustomMRubyClassAttribute));
                    if (attr != null)
                    {
                        exports.Add(t);
                    }
                }
            }
            return exports;
        }

#if false

        static public bool filterType(Type t, List<string> noUseList, List<string> uselist)
        {
            if (t.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
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

        public static List<Type> GetExportsType(string[] asemblyNames, string genAtPath)
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

                foreach (Type t in types)
                {
                    if (filterType(t, noUseList, uselist))
                    {
                        exports.Add(t);
                    }
                }
            }
            return exports;
        }
#endif
    }
}
