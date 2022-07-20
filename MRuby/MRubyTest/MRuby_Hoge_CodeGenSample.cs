#if true
using System;
using MRuby;
using System.Collections.Generic;
public class MRuby_Hoge_CodeGenSample {
	static RClass _cls;
	static mrb_value _cls_value;
	readonly Hoge.CodeGenSample obj;
	static CSObject Construct(mrb_state mrb, object obj) => ObjectCache.NewObject(mrb, _cls_value, obj);
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value _initialize(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample o;
			System.Int32 a1;
			Converter.checkType(l,0,out a1);
			System.String a2;
			Converter.checkType(l,1,out a2);
			o=new Hoge.CodeGenSample(a1,a2);
			ObjectCache.NewObjectByVal(l, _self, o);
			return DLL.mrb_nil_value();
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value GetStringValue(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			var ret=self.GetStringValue();
			return Converter.make_value(l, ret);
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value GetIntValue(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			var ret=self.GetIntValue();
			return Converter.make_value(l, ret);
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value OverloadedMethod(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			System.Int32 a1;
			Converter.checkType(l,0,out a1);
			var ret=self.OverloadedMethod(a1);
			return Converter.make_value(l, ret);
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value StaticMethod(mrb_state l, mrb_value _self) {
		try {
			System.Int32 a1;
			Converter.checkType(l,0,out a1);
			var ret=Hoge.CodeGenSample.StaticMethod(a1);
			return Converter.make_value(l, ret);
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value get_IntField(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			return Converter.make_value(l, self.IntField);
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value set_IntField(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			System.Int32 v;
			Converter.checkType(l,2,out v);
			self.IntField=v;
			return DLL.mrb_nil_value();
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value get_StringField(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			return Converter.make_value(l, self.StringField);
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value set_StringField(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			System.String v;
			Converter.checkType(l,2,out v);
			self.StringField=v;
			return DLL.mrb_nil_value();
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value get_IntProperty(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			return Converter.make_value(l, self.IntProperty);
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value set_IntProperty(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			int v;
			Converter.checkType(l,2,out v);
			self.IntProperty=v;
			return DLL.mrb_nil_value();
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value get_StringProperty(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			return Converter.make_value(l, self.StringProperty);
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	[MRuby.MonoPInvokeCallbackAttribute(typeof(MRubyCSFunction))]
	static public mrb_value set_StringProperty(mrb_state l, mrb_value _self) {
		try {
			Hoge.CodeGenSample self=(Hoge.CodeGenSample)Converter.checkSelf(l, _self);
			string v;
			Converter.checkType(l,2,out v);
			self.StringProperty=v;
			return DLL.mrb_nil_value();
		}
		catch(Exception e) {
			return Converter.error(l,e);
		}
	}
	static public void reg(mrb_state mrb) {
		RClass module = DLL.mrb_class_get(mrb, "Object");
		module = DLL.mrb_define_module_under(mrb, module, "Hoge");
		var baseClass = Converter.GetClass(mrb, "System.Object");
		_cls = DLL.mrb_define_class_under(mrb, module, "CodeGenSample", baseClass);
		_cls_value = DLL.mrb_obj_value(_cls.val);
		Converter.define_method(mrb, _cls, "initialize", _initialize, DLL.MRB_ARGS_OPT(4));
		TypeCache.AddType(typeof(Hoge.CodeGenSample), Construct);
		Converter.define_method(mrb, _cls, "getstringvalue", GetStringValue, DLL.MRB_ARGS_OPT(4));
		Converter.define_method(mrb, _cls, "getintvalue", GetIntValue, DLL.MRB_ARGS_OPT(4));
		Converter.define_method(mrb, _cls, "overloadedmethod", OverloadedMethod, DLL.MRB_ARGS_OPT(4));
		Converter.define_class_method(mrb, _cls, "staticmethod", StaticMethod, DLL.MRB_ARGS_OPT(4));
		Converter.define_property(mrb, _cls,"IntField",get_IntField,set_IntField,true);
		Converter.define_property(mrb, _cls,"StringField",get_StringField,set_StringField,true);
		Converter.define_property(mrb, _cls,"IntProperty",get_IntProperty,set_IntProperty,true);
		Converter.define_property(mrb, _cls,"StringProperty",get_StringProperty,set_StringProperty,true);
	}
}
#endif
