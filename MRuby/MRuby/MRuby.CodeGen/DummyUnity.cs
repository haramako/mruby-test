#if SLUA_STANDALONE
using System;

namespace UnityEngine
{

    public class Debug
    {
        public static void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void LogError(object msg)
        {

        }
    }

    public class YieldInstruction { }
    public class Coroutine { }
    public class Component { }
}

namespace UnityEditor
{

}
#endif
