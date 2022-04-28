using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRubyUnity
{
    [MRuby.CustomMRubyClass]
    public class IO
    {
        public static string Read(string path)
        {
            return System.IO.File.ReadAllText(path);
        }
    }

    [MRuby.CustomMRubyClass]
    public class Console
    {
        public Console()
        {

        }

        public void Write(string s)
        {
            Debug.Log(s);
        }
    }

    [MRuby.CustomMRubyClass]
    public class File
    {
        System.IO.FileStream f;

        public File(string path)
        {
            f = System.IO.File.Open(path, System.IO.FileMode.Open);
        }

        public static File Open(string path) => new File(path);

        public string Read()
        {
            byte[] buf = new byte[8192];
            var len = f.Read(buf, 0, buf.Length);
            return System.Text.Encoding.UTF8.GetString(buf, 0, len);
        }
    }
}

namespace Hoge
{
    [MRuby.CustomMRubyClass]
    public class CodeGenSample
    {
        public int IntField;
        public string StringField;

        public int IntProperty { get; set; }
        public string StringProperty { get; set; }

        public CodeGenSample(int i, string s)
        {
            IntField = i;
            StringField = s;
        }

        public string GetStringValue()
        {
            return "str";
        }

        public int GetIntValue()
        {
            return 99;
        }

        public int OverloadedMethod(int n)
        {
            return n;
        }

        public static int StaticMethod(int n)
        {
            return n;
        }

#if false
    public int OverloadedMethod(int n, int m)
    {
        return n + m;
    }

    public string OverloadedMethod(string s)
    {
        return s;
    }
#endif

    }
}
