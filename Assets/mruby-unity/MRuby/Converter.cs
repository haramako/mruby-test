using System;

namespace MRuby
{
    public static class Converter
    {
        static public bool checkEnum<T>(mrb_state mrb, mrb_value v, out T o) where T : struct
        {
            int i = (int)DLL.mrb_as_int(mrb, v);
            o = (T)Enum.ToObject(typeof(T), i);
            return true;
        }

        #region checkType
        static public void checkType(mrb_state mrb, mrb_value v, out sbyte r)
        {
            r = (sbyte)DLL.mrb_as_int(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out byte r)
        {
            r = (byte)DLL.mrb_as_int(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out char r)
        {
            r = (char)DLL.mrb_as_int(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out short r)
        {
            r = (short)DLL.mrb_as_int(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out ushort r)
        {
            r = (ushort)DLL.mrb_as_int(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out int r)
        {
            r = (int)DLL.mrb_as_int(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out uint r)
        {
            r = (uint)DLL.mrb_as_int(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out long r)
        {
            r = (long)DLL.mrb_as_int(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out ulong r)
        {
            r = (ulong)DLL.mrb_as_int(mrb, v);
        }

        public static void checkType(mrb_state mrb, mrb_value v, out float r)
        {
            r = (float)DLL.mrb_as_float(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out double r)
        {
            r = (double)DLL.mrb_as_float(mrb, v);
        }

        static public void checkType(mrb_state mrb, mrb_value v, out bool r)
        {
            r = DLL.mrb_bool(v);
        }

        static public void checkType(mrb_state l, mrb_value v, out string r)
        {
            r = DLL.mrb_as_string(l, v);
        }

        static public void checkType<T>(mrb_state l, mrb_value v, out T r) where T : class
        {
            // TODO
            r = null;
        }
        #endregion

#if false
        static public bool checkBinaryString(mrb_state l, int p, out byte[] bytes)
        {
			if (LuaDLL.lua_isstring(l, p))
            {
                bytes = LuaDLL.lua_tobytes(l, p);
                return true;
            }
            bytes = null;
            return false;
        }
#endif

#if false
		private static Type MonoType = typeof(Type).GetType();

		static public bool checkValueType<T>(mrb_state mrb, int p, out T v) where T : struct
		{
			v = (T)checkObj(l, p);
			return true;
		}

		static public bool checkNullable<T>(mrb_state mrb, int p, out Nullable<T> v) where T : struct
		{
			if (LuaDLL.lua_isnil(l, p))
				v = null;
			else
			{
				object o = checkVar(l, p, typeof(T));
				if (o == null) v = null;
				else v = new Nullable<T>((T)o);
			}
			return true;
		}

        static public void checkType<T>(mrb_state l, mrb_value v, out T r) where T : class
        {
            // TODO
            r = null;
        }
#endif

        unsafe static public void checkArray<T>(mrb_state mrb, mrb_value ary, out T[] r)
        {
            if (DLL.mrb_type(ary) == mrb_vtype.MRB_TT_ARRAY)
            {
                uint n = DLL.mrb_rarray_len(ary);
                mrb_value* ptr = DLL.mrb_rarray_ptr(ary);
                r = new T[n];
                for (int k = 0; k < n; k++)
                {
                    object obj = checkVar(mrb, ptr[k]);
                    if (obj is IConvertible)
                    {
                        r[k] = (T)Convert.ChangeType(obj, typeof(T));
                    }
                    else
                    {
                        r[k] = (T)obj;
                    }
                }
            }
            else
            {
                r = checkVar(mrb, ary) as T[];
            }
        }

        static public mrb_value make_value(mrb_state mrb, object val)
        {
            switch (val)
            {
                case Value v:
                    return v.val;
                case mrb_value v:
                    return v;
                case byte v:
                    return DLL.mrb_fixnum_value(v);
                case UInt16 v:
                    return DLL.mrb_fixnum_value(v);
                case Int16 v:
                    return DLL.mrb_fixnum_value(v);
                case UInt32 v:
                    return DLL.mrb_fixnum_value(v);
                case Int32 v:
                    return DLL.mrb_fixnum_value(v);
                case bool v:
                    return DLL.mrb_bool_value(v);
                case string v:
                    return DLL.mrb_str_new_cstr(mrb, v);
                case float v:
                    return DLL.mrb_float_value(mrb, v);
                case double v:
                    return DLL.mrb_float_value(mrb, v);
                default:
                    if (val == null)
                    {
                        return DLL.mrb_nil_value();
                    }
                    else if (VM.FindCache(mrb).ObjectCache.TryToValue(mrb, val, out var mrb_v))
                    {
                        return mrb_v;
                    }
                    else if (VM.FindCache(mrb).TypeCache.TryGetClass(val.GetType(), out TypeCache.ConstructorFunc constructor))
                    {
                        return constructor(mrb, val);
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
            }
        }

        static public object checkVar(mrb_state mrb, mrb_value v)
        {
            var type = DLL.mrb_type(v);
            switch (type)
            {
                case mrb_vtype.MRB_TT_INTEGER:
                    return DLL.mrb_as_int(mrb, v);
                case mrb_vtype.MRB_TT_STRING:
                    return DLL.mrb_as_string(mrb, v);
                case mrb_vtype.MRB_TT_TRUE:
                    return true;
                case mrb_vtype.MRB_TT_FALSE:
                    return false;
#if false
                case LuaTypes.LUA_TFUNCTION:
                    {
                        LuaFunction v;
                        LuaObject.checkType(l, p, out v);
                        return v;
                    }
                case LuaTypes.LUA_TTABLE:
                    {
                        if (isLuaValueType(l, p))
                        {
                            if (luaTypeCheck(l, p, "Vector2"))
                            {
                                Vector2 v;
                                checkType(l, p, out v);
                                return v;
                            }
                            else if (luaTypeCheck(l, p, "Vector3"))
                            {
                                Vector3 v;
                                checkType(l, p, out v);
                                return v;
                            }
                            else if (luaTypeCheck(l, p, "Vector4"))
                            {
                                Vector4 v;
                                checkType(l, p, out v);
                                return v;
                            }
                            else if (luaTypeCheck(l, p, "Quaternion"))
                            {
                                Quaternion v;
                                checkType(l, p, out v);
                                return v;
                            }
                            else if (luaTypeCheck(l, p, "Color"))
                            {
                                Color c;
                                checkType(l, p, out c);
                                return c;
                            }
                            Logger.LogError("unknown lua value type");
                            return null;
                        }
                        else if (isLuaClass(l, p))
                        {
                            return checkObj(l, p);
                        }
                        else
                        {
                            LuaTable v;
                            checkType(l, p, out v);
                            return v;
                        }
                    }
                case LuaTypes.LUA_TUSERDATA:
                    return LuaObject.checkObj(l, p);
                case LuaTypes.LUA_TTHREAD:
                    {
                        LuaThread lt;
                        LuaObject.checkType(l, p, out lt);
                        return lt;
                    }
#endif
                default:
                    return new Exception($"unsupported type {type}"); // TODO
            }
        }


#if false
        static public bool checkType(mrb_state mrb, int p, out LuaDelegate f)
        {
            LuaState state = LuaState.get(l);

            p = LuaDLL.lua_absindex(l, p);
            LuaDLL.luaL_checktype(l, p, LuaTypes.LUA_TFUNCTION);

            LuaDLL.lua_getglobal(l, DelgateTable);
            LuaDLL.lua_pushvalue(l, p);
            LuaDLL.lua_gettable(l, -2); // find function in __LuaDelegate table
            if (LuaDLL.lua_isnil(l, -1))
            { // not found
                LuaDLL.lua_pop(l, 1); // pop nil
                f = newDelegate(l, p);
            }
            else
            {
                int fref = LuaDLL.lua_tointeger(l, -1);
                LuaDLL.lua_pop(l, 1); // pop ref value;
                f = state.delgateMap[fref];
                if (f == null)
                {
                    f = newDelegate(l, p);
                }
            }
            LuaDLL.lua_pop(l, 1); // pop DelgateTable
            return true;
        }
#endif

#if false
        static public bool checkType(mrb_state mrb, int p, out LuaThread lt)
        {
            if (LuaDLL.lua_isnil(l, p))
            {
                lt = null;
                return true;
            }
            LuaDLL.luaL_checktype(l, p, LuaTypes.LUA_TTHREAD);
            LuaDLL.lua_pushvalue(l, p);
            int fref = LuaDLL.luaL_ref(l, LuaIndexes.LUA_REGISTRYINDEX);
            lt = new LuaThread(l, fref);
            return true;
        }
#endif

#if false
        static public bool checkType(mrb_state mrb, int p, out LuaFunction f)
        {
            if (LuaDLL.lua_isnil(l, p))
            {
                f = null;
                return true;
            }
            LuaDLL.luaL_checktype(l, p, LuaTypes.LUA_TFUNCTION);
            LuaDLL.lua_pushvalue(l, p);
            int fref = LuaDLL.luaL_ref(l, LuaIndexes.LUA_REGISTRYINDEX);
            f = new LuaFunction(l, fref);
return true;
        }
#endif

#if false
        static public bool checkType(mrb_state mrb, int p, out LuaTable t)
        {
            if (LuaDLL.lua_isnil(l, p))
            {
                t = null;
                return true;
            }
            LuaDLL.luaL_checktype(l, p, LuaTypes.LUA_TTABLE);
            LuaDLL.lua_pushvalue(l, p);
            int fref = LuaDLL.luaL_ref(l, LuaIndexes.LUA_REGISTRYINDEX);
            t = new LuaTable(l, fref);
            return true;
    }
#endif

        static public mrb_value error(mrb_state l, Exception e)
        {
            var excClass = DLL.mrb_class_get(l, "RuntimeError");
            var exc = DLL.mrb_exc_new_str(l, excClass, DLL.mrb_str_new_cstr(l, e.Message));
            return exc;
        }

        static public mrb_value error(mrb_state l, string err)
        {
#if false
			LuaDLL.lua_pushboolean(l, false);
			LuaDLL.lua_pushstring(l, err);
#endif
            return default;
        }

        static public int error(mrb_state mrb, string err, params object[] args)
        {
#if false
			err = string.Format(err, args);
			LuaDLL.lua_pushboolean(l, false);
			LuaDLL.lua_pushstring(l, err);
#endif
            return 2;
        }

        public static T checkSelf<T>(mrb_state l)
        {
#if false
			object o = checkObj(l, 1);
			if (o != null)
			{
				return (T)o;
			}
			throw new Exception("arg 1 expect self, but get null");
#endif
            return default;
        }

        public static object checkSelf(mrb_state l, mrb_value self)
        {
#if false
			object o = checkObj(l, 1);
			if (o == null)
				throw new Exception("expect self, but get null");
			return o;
#else
            return VM.FindCache(l).ObjectCache.GetObject(l, self);
#endif
        }

        public static bool matchType(mrb_state mrb, mrb_value v, Type t)
        {
            var type = DLL.mrb_type(v);
            switch (type)
            {
                case mrb_vtype.MRB_TT_INTEGER:
                    return t == typeof(int);
                case mrb_vtype.MRB_TT_FLOAT:
                    return t == typeof(float);
                case mrb_vtype.MRB_TT_STRING:
                    return t == typeof(string);
                case mrb_vtype.MRB_TT_TRUE:
                    return t == typeof(bool);
                case mrb_vtype.MRB_TT_FALSE:
                    return t == typeof(bool);
                case mrb_vtype.MRB_TT_ARRAY:
                    return t == typeof(Array);
                case mrb_vtype.MRB_TT_OBJECT:
                    return false;
                default:
                    return false;
            }
        }

#if false
        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, int p, Type t1)
        {
            return matchType(mrb, args[p], t1);
        }
#endif

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, Type t0)
        {
            return matchType(mrb, args[0], t0);
        }

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, Type t0, Type t1)
        {
            return matchType(mrb, args[0], t0) && matchType(mrb, args[1], t1);
        }

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, Type t0, Type t1, Type t2)
        {
            return matchType(mrb, args[0], t0) && matchType(mrb, args[1], t1) && matchType(mrb, args[2], t2);
        }

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, Type t0, Type t1, Type t2, Type t3)
        {
            return matchType(mrb, args[0], t0) && matchType(mrb, args[1], t1) && matchType(mrb, args[2], t2) && matchType(mrb, args[3], t3);
        }

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, Type t0, Type t1, Type t2, Type t3, Type t4)
        {
            return matchType(mrb, args[0], t0) && matchType(mrb, args[1], t1) && matchType(mrb, args[2], t2) && matchType(mrb, args[3], t3) && matchType(mrb, args[4], t4);
        }

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, Type t0, Type t1, Type t2, Type t3, Type t4, Type t5)
        {
            return matchType(mrb, args[0], t0) && matchType(mrb, args[1], t1) && matchType(mrb, args[2], t2) && matchType(mrb, args[3], t3) && matchType(mrb, args[4], t4) && matchType(mrb, args[5], t5);
        }

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, params Type[] t)
        {
            for (int i = 0; i < t.Length; ++i)
            {
                if (!matchType(mrb, args[i], t[0]))
                {
                    return false;
                }
            }
            return true;
        }

        public static string ToString(mrb_state mrb, mrb_value val)
        {
            return AsString(mrb, Send(mrb, val, "to_s"));
        }

        public static string AsString(mrb_state mrb, mrb_value val)
        {
            var len = DLL.mrb_string_len(mrb, val);
            var buf = new byte[len];
            DLL.mrb_string_buf(mrb, val, buf, len);
            return System.Text.Encoding.UTF8.GetString(buf);
        }

        public static mrb_value Send(mrb_state mrb, mrb_value val, string methodName)
        {
            var r = DLL.mrb_funcall_argv(mrb, val, methodName, 0, null);
            var exc = DLL.mrb_mrb_state_exc(mrb);
            if (!exc.IsNil)
            {
                throw new Exception(new Value(mrb, exc).ToString());
            }
            else
            {
                return r;
            }
        }

        static mrb_value[] argsCache = new mrb_value[16];

        public static mrb_value Send(mrb_state mrb, mrb_value val, string methodName, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                argsCache[i] = new Value(mrb, args[i]).val;
            }
            return DLL.mrb_funcall_argv(mrb, val, methodName, args.Length, argsCache);
        }

        public static mrb_value Exec(mrb_state mrb, string src, string filename = null)
        {
#if false
            mrbc_context ctx = DLL.mrbc_context_new(mrb);
            if (filename != null)
            {
                DLL.mrbc_filename(mrb, ctx, filename);
            }
            var r = DLL.mrb_load_string_cxt(mrb, src, ctx);
            var exc = DLL.mrb_mrb_state_exc(mrb);

            if (!DLL.mrb_nil_p(exc))
            {
                throw new Exception(Converter.ToString(mrb, exc));
            }

            DLL.mrbc_context_free(mrb, ctx);
#else
            var r = DLL.mrb_load_string_filename(mrb, src, filename);
            var exc = DLL.mrb_mrb_state_exc(mrb);

            if (!DLL.mrb_nil_p(exc))
            {
                throw new Exception(Converter.ToString(mrb, exc));
            }
#endif

            return r;
        }

        public static RClass GetClass(mrb_state mrb, string[] names)
        {
            mrb_value module = DLL.mrb_obj_value(DLL.mrb_class_get(mrb, "Object").val);
            for (int i = 0; i < names.Length; i++)
            {
                module = DLL.mrb_const_get(mrb, module, DLL.mrb_intern_cstr(mrb, names[i]));
            }
            return DLL.mrb_class_ptr(module);
        }

        public static RClass GetClass(mrb_state mrb, string name)
        {
            if (name == null || name == "System.Object" || name == "UnityEngine.MonoBehaviour")
            {
                return DLL.mrb_class_get(mrb, "Object");
            }
            else
            {
                return GetClass(mrb, name.Split("::"));
            }
        }

        public static void CheckArgc(long argc, int min, int max)
        {
            if (min == max && argc != min)
            {
                throw new Exception($"wrong number of arguments (given {argc}, expected {min})");
            }
            else if (argc > max || argc < min)
            {
                throw new Exception($"wrong number of arguments (given {argc}, expected {min}..{max})");
            }
        }

        public struct ArenaLock : IDisposable
        {
            mrb_state mrb;
            int arenaIndex;

            public ArenaLock(mrb_state _mrb)
            {
                mrb = _mrb;
                arenaIndex = DLL.mrb_gc_arena_save(mrb);
            }

            public void Dispose()
            {
                if (arenaIndex != -1)
                {
                    DLL.mrb_gc_arena_restore(mrb, arenaIndex);
                    arenaIndex = -1;
                }
            }
        }

        public static ArenaLock LockArena(mrb_state mrb)
        {
            return new ArenaLock(mrb);
        }
    }
}
