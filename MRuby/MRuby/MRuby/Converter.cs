using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace MRuby
{
    public static class Converter
    {
#if false
        #region enum
		static public bool checkEnum<T>(IntPtr l, int p, out T o) where T : struct
		{
			int i = (int)LuaDLL.luaL_checkinteger(l, p);
			o = (T)Enum.ToObject(typeof(T), i);

			return true;
		}

		public static void pushEnum(IntPtr l, int e)
		{
			pushValue(l, e);
		}
        #endregion

		//#region Integral Types
        #region sbyte
		public static bool checkType(IntPtr l, int p, out sbyte v)
		{
			v = (sbyte)LuaDLL.luaL_checkinteger(l, p);
			return true;
		}

		public static void pushValue(IntPtr l, sbyte v)
		{
			LuaDLL.lua_pushinteger(l, v);
		}

        #endregion

        #region byte
		static public bool checkType(IntPtr l, int p, out byte v)
		{
			v = (byte)LuaDLL.luaL_checkinteger(l, p);
			return true;
		}

		public static void pushValue(IntPtr l, byte i)
		{
			LuaDLL.lua_pushinteger(l, i);
		}

		// why doesn't have a checkArray<byte[]> function accept lua string?
		// I think you should did a Buffer class to wrap byte[] pass/accept between mono and lua vm
        #endregion

        #region char
		static public bool checkType(IntPtr l, int p, out char c)
		{
			c = (char)LuaDLL.luaL_checkinteger(l, p);
			return true;
		}

		public static void pushValue(IntPtr l, char v)
		{
			LuaDLL.lua_pushinteger(l, v);
		}

		static public bool checkArray(IntPtr l, int p, out char[] pars)
		{
			LuaDLL.luaL_checktype(l, p, LuaTypes.LUA_TSTRING);
			string s;
			checkType(l, p, out s);
			pars = s.ToCharArray();
			return true;
		}
        #endregion

        #region short
		static public bool checkType(IntPtr l, int p, out short v)
		{
			v = (short)LuaDLL.luaL_checkinteger(l, p);
			return true;
		}

		public static void pushValue(IntPtr l, short i)
		{
			LuaDLL.lua_pushinteger(l, i);
		}
        #endregion

        #region ushort
		static public bool checkType(IntPtr l, int p, out ushort v)
		{
			v = (ushort)LuaDLL.luaL_checkinteger(l, p);
			return true;
		}

		public static void pushValue(IntPtr l, ushort v)
		{
			LuaDLL.lua_pushinteger(l, v);
		}

        #endregion

        #region interface
		static public void pushInterface(IntPtr l, object i, Type t)
		{
			ObjectCache oc = ObjectCache.get(l);
			oc.pushInterface(l, i, t);
		}
        #endregion
#endif

        #region int
        static public bool checkType(mrb_state l, int p, out int v)
        {
#if false
			v = (int)LuaDLL.luaL_checkinteger(l, p);
            return true;
#else
            unsafe
            {
                mrb_value* args = DLL.mrb_get_argv(l);
                v = (int)DLL.mrb_as_int(l, args[p]);
                return true;
            }
#endif
        }

        public static void pushValue(mrb_state l, int i)
        {
#if false
			LuaDLL.lua_pushinteger(l, i);
#endif
        }

        #endregion

#if false
        #region uint
		static public bool checkType(IntPtr l, int p, out uint v)
		{
			v = (uint)LuaDLL.luaL_checkinteger(l, p);
			return true;
		}

		public static void pushValue(IntPtr l, uint o)
		{
			LuaDLL.lua_pushnumber(l, o);
		}
        #endregion

        #region long
		static public bool checkType(IntPtr l, int p, out long v)
		{
#if LUA_5_3
            v = (long)LuaDLL.luaL_checkinteger(l, p);
#else
			v = (long)LuaDLL.luaL_checknumber(l, p);
#endif
			return true;
		}

		public static void pushValue(IntPtr l, long i)
		{
#if LUA_5_3
            LuaDLL.lua_pushinteger(l,i);
#else
			LuaDLL.lua_pushnumber(l, i);
#endif
		}

        #endregion

        #region ulong
		static public bool checkType(IntPtr l, int p, out ulong v)
		{
#if LUA_5_3
			v = (ulong)LuaDLL.luaL_checkinteger(l, p);
#else
			v = (ulong)LuaDLL.luaL_checknumber(l, p);
#endif
			return true;
		}

		public static void pushValue(IntPtr l, ulong o)
		{
#if LUA_5_3
			LuaDLL.lua_pushinteger(l, (long)o);
#else
			LuaDLL.lua_pushnumber(l, o);
#endif
		}
        #endregion

#endif
        //#endregion


        #region Floating-Point Types
        #region float
        public static bool checkType(mrb_state l, int p, out float v)
        {
            unsafe
            {
                mrb_value* args = DLL.mrb_get_argv(l);
                v = (int)DLL.mrb_as_float(l, args[p]);
                return true;
            }
        }

#if false
		public static void pushValue(IntPtr l, float o)
		{
			LuaDLL.lua_pushnumber(l, o);
		}
#endif

        #endregion

#if false
        #region double
		static public bool checkType(IntPtr l, int p, out double v)
		{
			v = LuaDLL.luaL_checknumber(l, p);
			return true;
		}

		public static void pushValue(IntPtr l, double d)
		{
			LuaDLL.lua_pushnumber(l, d);
		}

        #endregion
#endif
        #endregion

#if false
        #region bool
		static public bool checkType(IntPtr l, int p, out bool v)
		{
			LuaDLL.luaL_checktype(l, p, LuaTypes.LUA_TBOOLEAN);
			v = LuaDLL.lua_toboolean(l, p);
			return true;
		}

		public static void pushValue(IntPtr l, bool b)
		{
			LuaDLL.lua_pushboolean(l, b);
		}

        #endregion

#endif

        #region string
        static public bool checkType(mrb_state l, int p, out string v)
        {
#if false
			if (LuaDLL.lua_isuserdata(l, p) > 0)
            {
                object o = checkObj(l, p);
                if (o is string)
                {
                    v = o as string;
                    return true;
                }
            }
            else if (LuaDLL.lua_isstring(l, p))
            {
                v = LuaDLL.lua_tostring(l, p);
                return true;
            }

            v = null;
            return false;
#else
            unsafe
            {
                mrb_value* args = DLL.mrb_get_argv(l);
                v = DLL.mrb_as_string(l, args[p]);
            }
            return true;
#endif
        }

        static public bool checkBinaryString(mrb_state l, int p, out byte[] bytes)
        {
#if false
			if (LuaDLL.lua_isstring(l, p))
            {
                bytes = LuaDLL.lua_tobytes(l, p);
                return true;
            }
            bytes = null;
            return false;
#else
            bytes = null;
            return true;
#endif
        }

        public static void pushValue(mrb_state l, string s)
        {
#if false
			LuaDLL.lua_pushstring(l, s);
#endif
        }


        #endregion

#if false
        #region IntPtr
		static public bool checkType(IntPtr l, int p, out IntPtr v)
		{
			v = LuaDLL.lua_touserdata(l, p);
			return true;
		}
        #endregion

        #region LuaType
		static public bool checkType(IntPtr l, int p, out LuaDelegate f)
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

		static public bool checkType(IntPtr l, int p, out LuaThread lt)
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

		static public bool checkType(IntPtr l, int p, out LuaFunction f)
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

		static public bool checkType(IntPtr l, int p, out LuaTable t)
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

		public static void pushValue(IntPtr l, LuaCSFunction f)
		{
			LuaState.pushcsfunction(l, f);
		}

		public static void pushValue(IntPtr l, LuaTable t)
		{
			if (t == null)
				LuaDLL.lua_pushnil(l);
			else
				t.push(l);
		}
        #endregion

        #region Type
		private static Type MonoType = typeof(Type).GetType();

		public static Type FindType(string qualifiedTypeName)
		{
			Type t = Type.GetType(qualifiedTypeName);

			if (t != null)
			{
				return t;
			}
			else
			{
				Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for (int n = 0; n < Assemblies.Length; n++)
				{
					Assembly asm = Assemblies[n];
					t = asm.GetType(qualifiedTypeName);
					if (t != null)
						return t;
				}
				return null;
			}
		}


		static public bool checkType(IntPtr l, int p, out Type t)
		{
			string tname = null;
			LuaTypes lt = LuaDLL.lua_type(l, p);
			switch (lt)
			{
				case LuaTypes.LUA_TUSERDATA:
					object o = checkObj(l, p);
					if (o.GetType() != MonoType)
						throw new Exception(string.Format("{0} expect Type, got {1}", p, o.GetType().Name));
					t = (Type)o;
					return true;
				case LuaTypes.LUA_TTABLE:
					LuaDLL.lua_pushstring(l, "__type");
					LuaDLL.lua_rawget(l, p);
					if (!LuaDLL.lua_isnil(l, -1))
					{
						t = (Type)checkObj(l, -1);
						LuaDLL.lua_pop(l, 1);
						return true;
					}
					else
					{
						LuaDLL.lua_pushstring(l, "__fullname");
						LuaDLL.lua_rawget(l, p);
						tname = LuaDLL.lua_tostring(l, -1);
						LuaDLL.lua_pop(l, 2);
					}
					break;

				case LuaTypes.LUA_TSTRING:
					checkType(l, p, out tname);
					break;
			}

			if (tname == null)
				throw new Exception("expect string or type table");

			t = LuaObject.FindType(tname);
			if (t != null && lt == LuaTypes.LUA_TTABLE)
			{
				LuaDLL.lua_pushstring(l, "__type");
				pushLightObject(l, t);
				LuaDLL.lua_rawset(l, p);
			}
			return t != null;
		}
        #endregion

        #region struct
		static public bool checkValueType<T>(IntPtr l, int p, out T v) where T : struct
		{
			v = (T)checkObj(l, p);
			return true;
		}
        #endregion

		static public bool checkNullable<T>(IntPtr l, int p, out Nullable<T> v) where T : struct
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
#endif

        #region object
        static public bool checkType<T>(mrb_state l, int p, out T o) where T : class
        {
#if false
			object obj = checkVar(l, p);
			if (obj == null)
			{
				o = null;
				return true;
			}

			o = obj as T;
			if (o == null)
				throw new Exception(string.Format("arg {0} is not type of {1}", p, typeof(T).Name));

			return true;
#else
            o = null;
            return false;
#endif
        }
        #endregion

        static public bool checkArray<T>(mrb_state mrb, int p, out T[] ta)
        {
            unsafe
            {
                mrb_value* args = DLL.mrb_get_argv(mrb);
                mrb_value ary = args[p];
                if (DLL.mrb_type(ary) == mrb_vtype.MRB_TT_ARRAY)
                {
                    uint n = DLL.mrb_rarray_len(ary);
                    mrb_value* ptr = DLL.mrb_rarray_ptr(ary);
                    ta = new T[n];
                    for (int k = 0; k < n; k++)
                    {
                        object obj = checkVar(mrb, ptr[k]);
                        if (obj is IConvertible)
                        {
                            ta[k] = (T)Convert.ChangeType(obj, typeof(T));
                        }
                        else
                        {
                            ta[k] = (T)obj;
                        }
                    }
                    return true;
                }
                else
                {
                    Array array = checkVar(mrb, ary) as Array;
                    ta = array as T[];
                    return ta != null;
                }
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
                    else if (MrbState.FindCache(mrb).ObjectCache.TryToValue(mrb, val, out var mrb_v))
                    {
                        return mrb_v;
                    }
                    else if (MrbState.FindCache(mrb).TypeCache.TryGetClass(val.GetType(), out TypeCache.ConstructorFunc constructor))
                    {
                        var obj = constructor(mrb, val);
                        return obj.val;
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
#if !SLUA_STANDALONE
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
#endif
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
                    //return null;
            }
        }


#if false
        static public bool checkType(IntPtr l, int p, out LuaDelegate f)
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
        static public bool checkType(IntPtr l, int p, out LuaThread lt)
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
        static public bool checkType(IntPtr l, int p, out LuaFunction f)
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
        static public bool checkType(IntPtr l, int p, out LuaTable t)
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

        public static void pushValue(IntPtr l, MRubyCSFunction f)
        {
#if false
            LuaState.pushcsfunction(l, f);
#endif
        }

#if false
        public static void pushValue(IntPtr l, LuaTable t)
        {
            if (t == null)
                LuaDLL.lua_pushnil(l);
            else
                t.push(l);
}
#endif

        public static void pushValue(IntPtr l, object o)
        {
#if false
			pushVar(l, o);
#endif
        }

        public static void pushValue(IntPtr l, Array a)
        {
#if false
			pushObject(l, a);
#endif
        }

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

        static public int error(IntPtr l, string err, params object[] args)
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
            return MrbState.FindCache(l).ObjectCache.GetObject(l, self);
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

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, int p, Type t1)
        {
            return matchType(mrb, args[p], t1);
        }

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, Type t1)
        {
            return matchType(mrb, args[0], t1);
        }

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, Type t1, Type t2)
        {
            return matchType(mrb, args[0], t1) && matchType(mrb, args[1], t2);
        }

        public static unsafe bool matchType(mrb_state mrb, mrb_value* args, Type t1, Type t2, Type t3)
        {
            return matchType(mrb, args[0], t1) && matchType(mrb, args[1], t2) && matchType(mrb, args[2], t3);
        }

#if false
        public static bool matchType(mrb_state mrb, int total, int from, Type t1, Type t2, Type t3, Type t4)
        {
            if (total - from + 1 != 4)
                return false;

            return matchType(l, from, t1) && matchType(l, from + 1, t2) && matchType(l, from + 2, t3) && matchType(l, from + 3, t4);
        }

        public static bool matchType(mrb_state mrb, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5)
        {
            if (total - from + 1 != 5)
                return false;

            return matchType(l, from, t1) && matchType(l, from + 1, t2) && matchType(l, from + 2, t3) && matchType(l, from + 3, t4)
                && matchType(l, from + 4, t5);
        }

        public static bool matchType
            (mrb_state mrb, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6)
        {
            if (total - from + 1 != 6)
                return false;

            return matchType(l, from, t1) && matchType(l, from + 1, t2) && matchType(l, from + 2, t3) && matchType(l, from + 3, t4)
                && matchType(l, from + 4, t5)
                && matchType(l, from + 5, t6);
        }

        public static bool matchType
            (mrb_state mrb, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6, Type t7)
        {
            if (total - from + 1 != 7)
                return false;

            return matchType(l, from, t1) && matchType(l, from + 1, t2) && matchType(l, from + 2, t3) && matchType(l, from + 3, t4)
                && matchType(l, from + 4, t5)
                && matchType(l, from + 5, t6)
                && matchType(l, from + 6, t7);
        }

        public static bool matchType
            (mrb_state mrb, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6, Type t7, Type t8)
        {
            if (total - from + 1 != 8)
                return false;

            return matchType(l, from, t1) && matchType(l, from + 1, t2) && matchType(l, from + 2, t3) && matchType(l, from + 3, t4)
                && matchType(l, from + 4, t5)
                && matchType(l, from + 5, t6)
                && matchType(l, from + 6, t7)
                && matchType(l, from + 7, t8);
        }


        public static bool matchType
            (mrb_state mrb, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6, Type t7, Type t8, Type t9)
        {
            if (total - from + 1 != 9)
                return false;

            return matchType(l, from, t1) && matchType(l, from + 1, t2) && matchType(l, from + 2, t3) && matchType(l, from + 3, t4)
                && matchType(l, from + 4, t5)
                && matchType(l, from + 5, t6)
                && matchType(l, from + 6, t7)
                && matchType(l, from + 7, t8)
                && matchType(l, from + 8, t9);
        }

        public static bool matchType
            (mrb_state mrb, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6, Type t7, Type t8, Type t9, Type t10)
        {
            if (total - from + 1 != 10)
                return false;

            return matchType(l, from, t1) && matchType(l, from + 1, t2) && matchType(l, from + 2, t3) && matchType(l, from + 3, t4)
                && matchType(l, from + 4, t5)
                    && matchType(l, from + 5, t6)
                    && matchType(l, from + 6, t7)
                    && matchType(l, from + 7, t8)
                    && matchType(l, from + 8, t9)
                    && matchType(l, from + 9, t10);
        }

        public static bool matchType(mrb_state mrb, int total, int from, params Type[] t)
        {
            if (total - from + 1 != t.Length)
                return false;

            for (int i = 0; i < t.Length; ++i)
            {
                if (!matchType(l, from + i, t[i]))
                    return false;
            }

            return true;
        }

        public static bool matchType(mrb_state mrb, int total, int from, ParameterInfo[] pars)
        {
#if false
			if (total - from + 1 != pars.Length)
				return false;

			for (int n = 0; n < pars.Length; n++)
			{
				int p = n + from;
				LuaTypes t = LuaDLL.lua_type(l, p);
				if (!matchType(l, p, t, pars[n].ParameterType))
					return false;
			}
#endif
            return true;
        }
#endif

        public static void define_method(mrb_state mrb, RClass cls, string funcName, MRubyCSFunction func, mrb_aspec aspec)
        {
            DLL.mrb_define_method(mrb, cls, funcName, func, aspec);
        }

        public static void define_class_method(mrb_state mrb, RClass cls, string funcName, MRubyCSFunction func, mrb_aspec aspec)
        {
            DLL.mrb_define_class_method(mrb, cls, funcName, func, aspec);
        }

        public static void define_property(mrb_state mrb, RClass cls, string name, MRubyCSFunction getter, MRubyCSFunction setter, bool isInstance)
        {
            DLL.mrb_define_method(mrb, cls, name, getter, DLL.MRB_ARGS_NONE());
            DLL.mrb_define_method(mrb, cls, name + "=", setter, DLL.MRB_ARGS_REQ(1));
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

        public static mrb_value Exec(mrb_state mrb, string src)
        {
            mrbc_context ctx = DLL.mrbc_context_new(mrb);
            var r = DLL.mrb_load_string_cxt(mrb, src, ctx);
            var exc = DLL.mrb_mrb_state_exc(mrb);

            if (!DLL.mrb_nil_p(exc))
            {
                throw new Exception(Converter.ToString(mrb, exc));
            }

            DLL.mrbc_context_free(mrb, ctx);

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
