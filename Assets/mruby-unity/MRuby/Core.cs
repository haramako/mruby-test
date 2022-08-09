using System.IO;
using MRuby;
using IO_File = System.IO.File;

namespace MRubyUnity
{
    public static class Core
    {
        public static string[] LoadPath = new string[] { ".", "RubyLib" };
        public static string currentLoadingFile;

        public static mrb_value _require(mrb_state mrb, mrb_value _self)
        {
            unsafe
            {
                System.String name;
                var _argv = DLL.mrb_get_argv(mrb);
                Converter.checkType(mrb, _argv[0], out name);
                Require(mrb, name);
                return DLL.mrb_nil_value();
            }
        }

        public static void Require(mrb_state mrb, string name)
        {
            if (!name.EndsWith(".rb"))
            {
                name += ".rb";
            }
            if (name.StartsWith("./"))
            {
                if (requireInner(mrb, Path.Combine(Path.GetDirectoryName(currentLoadingFile), name), Path.GetDirectoryName(currentLoadingFile)))
                {
                    return;
                }
            }
            else
            {
                foreach (var path in LoadPath)
                {
                    if (requireInner(mrb, Path.Combine(path, name), path))
                    {
                        return;
                    }
                }
            }
            throw new System.Exception($"{name} not found");
        }

        private static bool requireInner(mrb_state mrb, string fullPath, string baseDir)
        {
            if (IO_File.Exists(fullPath))
            {
                var src = IO_File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                var old = currentLoadingFile;
                currentLoadingFile = fullPath;
                var relativePath = fullPath.Substring(baseDir.Length + 1);
                try
                {
                    Converter.Exec(mrb, src, relativePath);
                }
                finally
                {
                    currentLoadingFile = old;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

    }

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
        static Console instance;
        public Console()
        {
            instance = this;
        }

        public void Write(string s)
        {
#if !UNITY_2020_1_OR_NEWER
            System.Console.WriteLine(s);
#else
            UnityEngine.Debug.Log(s);
#endif
        }

        public void Flush()
        {

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

        public void Flush()
        {
        }
    }
}

