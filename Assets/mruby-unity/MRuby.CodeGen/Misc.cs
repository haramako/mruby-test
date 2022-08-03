using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#if !UNITY_2020_1_OR_NEWER
namespace UnityEngine
{
    public class YieldInstruction { }
    public class Coroutine { }
    public class Component { }
}
#endif

namespace MRuby.CodeGen
{

    public static class Naming
    {
        public static string RubyName(string name)
        {
            return name.Replace(".", "::").Replace("+", "::");
        }

        public static string ToSnakeCase(string name)
        {
            // Special names.
            switch (name)
            {
                case "ToString":
                    return "to_s";
            }

            var sb = new StringBuilder();
            var prevIsUpper = true;
            var prevIsUnderscore = false;
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (prevIsUpper || prevIsUnderscore)
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
                    prevIsUnderscore = (c == '_');
                }
            }
            return sb.ToString();
        }

        public static string GenericBaseName(Type t)
        {
            string n = t.ToString();
            if (n.IndexOf('[') > 0)
            {
                n = n.Substring(0, n.IndexOf('['));
            }
            return n.Replace("+", ".");
        }

        public static string GenericName(Type t, string sep = "_")
        {
            try
            {
                Type[] tt = t.GetGenericArguments();
                string ret = "";
                for (int n = 0; n < tt.Length; n++)
                {
                    string dt = TypeUtil.SimpleTypeName(tt[n]);
                    ret += dt;
                    if (n < tt.Length - 1)
                        ret += sep;
                }
                return ret;
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                return "";
            }
        }

        /// <summary>
        /// C#のfullnameからcode上の名前に変換する
        /// </summary>
        /// <param name="fullname"></param>
        /// <returns></returns>
        public static string CodeName(string fullname)
        {
            return fullname.Replace('+', '.');
        }

    }

    public static class Logger
    {
#if false
        public static TextWriter Console;
        static Logger()
        {
            Console = File.CreateText("log.txt");
        }
#endif


        public static void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void Log(string fmt, params object[] args)
        {
            Console.WriteLine(fmt, args);
        }

        public static void Log(object msg)
        {
            Console.WriteLine(msg);
        }

        public static void LogError(string msg)
        {
            Console.WriteLine(msg);
        }
        public static void LogError(string fmt, params object[] args)
        {
            Console.WriteLine(fmt, args);
        }

        public static void LogError(object msg)
        {
            Console.WriteLine(msg);
        }
    }

    class CodeWriter : IDisposable
    {
        public enum EOL
        {
            Native,
            CRLF,
            CR,
            LF,
        }

        public static EOL eol = EOL.Native;

        int indent = 0;
        StreamWriter w;

        public CodeWriter(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            w = new StreamWriter(path, false, Encoding.UTF8);
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

        public void Dispose()
        {
            if (w != null)
            {
                w.Close();
                w = null;
            }
        }

        Regex NewLinePattern = new Regex(@"\r\n?|\n|\r");

        public void Write(string fmt, params object[] args)
        {
            fmt = NewLinePattern.Replace(fmt, NewLine);

            if (fmt.StartsWith("}")) indent--;

            for (int n = 0; n < indent; n++)
            {
                w.Write("\t");
            }

            if (args.Length == 0)
            {
                w.WriteLine(fmt);
            }
            else
            {
                string line = string.Format(fmt, args);
                w.WriteLine(line);
            }

            if (fmt.EndsWith("{")) indent++;
        }
    }

    public class RegistryPrinter
    {
        int indent;
        int level;

        public RegistryPrinter(int _level = 1)
        {
            level = _level;
        }

        void write(string fmt, params object[] args)
        {
            Console.Write(new string(' ', indent * 2));
            Console.WriteLine(fmt, args);
        }

        public void PrintRegistry(Registry reg)
        {
            foreach (var cls in reg.AllDescs())
            {
                //write(new string('=', 40));
                PrintClassDesc(cls);
            }
        }

        public void PrintClassDesc(ClassDesc cls)
        {
            if (cls.IsNamespace)
            {
                write("{1} N {0,-20}", cls.FullName, cls.PopCountFromExport);
            }
            else
            {
                write("{1} C {0,-20}", cls.FullName, cls.PopCountFromExport);
            }

            if (level >= 1)
            {
                indent += 2;
                foreach (var m in cls.MethodDescs.Values)
                {
                    PrintMethodDesc(m);
                }
                foreach (var f in cls.Fields.Values)
                {
                    PrintField(f);
                }
                indent -= 2;
            }
        }

        public void PrintMethodDesc(MethodDesc m)
        {
            var (min, max, hasParamArray) = m.ParameterNum();
            var mark = m.IsConstructor ? "c" : (m.IsStatic ? "s" : " ");
            var param = hasParamArray ? $"{min}.." : ((min == max) ? $"{min}" : $"{min}..{max}");
            write($"{mark}  {m.Name}({param}) [{m.Methods.Count}]");

            if (m.IsOverloaded && level >= 2)
            {
                indent++;
                foreach (var method in m.Methods)
                {
                    var extension = method.IsExtension ? "x" : " ";
                    write($"  {extension} {method.Info}");
                    //write($"  {extension} {method} {method.Info.Attributes}");
                }
                indent--;
            }
        }

        public void PrintField(FieldDesc f)
        {
            var kind = f.IsProperty ? "p" : "f";
            var mark = (f.IsStatic ? "s" : " ");
            var canRead = f.CanRead ? "r" : "-";
            var canWrite = f.CanWrite ? "w" : "-";
            write($"{kind}{mark} {f.Name} {canRead}{canWrite}");
        }
    }

}
