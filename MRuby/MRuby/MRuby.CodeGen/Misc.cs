using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
    }

    public static class Logger
    {
        public static void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void LogError(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void LogError(object msg)
        {
            Console.WriteLine(msg);
        }
    }

    class CodeWriter : IDisposable
    {
        public static EOL eol = MRubySetting.Instance.eol;

        int indent = 0;
        StreamWriter w;

        public CodeWriter(string path)
        {
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

}
