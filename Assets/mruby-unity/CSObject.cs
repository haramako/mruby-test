using System;
#if !SLUA_STANDALONE
using UnityEngine;
#endif

namespace MRuby
{
    public abstract class CSObject
    {
        public mrb_value val;

        public static bool isImplByLua(Type t)
        {
#if !SLUA_STANDALONE
            return t == typeof(Color)
                || t == typeof(Vector2)
                || t == typeof(Vector3)
                || t == typeof(Vector4)
                || t == typeof(Quaternion);
#else
		    return false;
#endif
        }
    }
}
