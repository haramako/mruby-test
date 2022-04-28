using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MRuby;
using System.IO;

public class Test : MonoBehaviour
{
    public void OnButtonClick()
    {
        MrbState _mrb = new MrbState();
        var mrb = _mrb.mrb;
        var arena = DLL.mrb_gc_arena_save(mrb);
        Converter.sym_objid = DLL.mrb_intern_cstr(mrb, "objid");

        mrb_value r;

#if false
        MRuby_Character.RegisterClass(mrb);

        var r = mrb.LoadString("2+10");

        Debug.Log(r.ToString(mrb));

        var r3 = mrb.LoadString("Character.new('a',1).show");

        Debug.Log(r3.AsString(mrb));

        var r4 = new Value(mrb, new Character("hoge", 3));

        Debug.Log(r4.Send(mrb, "show").ToString(mrb));
#endif


#if false
        MRuby_Hoge_CodeGenSample.reg(_mrb);

        r = Converter.Exec(mrb, "Hoge::CodeGenSample.new(1,'2')");

        Debug.Log(Converter.ToString(mrb, r));

        Debug.Log(Converter.ToString(mrb, Converter.Send(mrb, r, "GetIntValue")));


        r = Converter.Exec(mrb, "Hoge::CodeGenSample.new(3,'2').IntField");
        Debug.Log(Converter.ToString(mrb, r));

        r = Converter.Exec(mrb, "Hoge::CodeGenSample.StaticMethod(9)");
        Debug.Log(Converter.ToString(mrb, r));
#endif

        MRuby_MRubyUnity_Console.reg(_mrb);

        var src = File.ReadAllText("RubyLib\\prelude.rb");
        r = Converter.Exec(mrb, src);

        DLL.mrb_gc_arena_restore(mrb, arena);

    }
}

