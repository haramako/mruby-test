using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MRuby;

public class Test : MonoBehaviour
{
    public void OnButtonClick()
    {
        MrbState _mrb = new MrbState();
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

        var mrb = _mrb.mrb;

        mrbc_context ctx = DLL.mrbc_context_new(mrb);
        Converter.sym_objid = DLL.mrb_intern_cstr(mrb, "objid");
        MRuby_CodeGenSample.reg(_mrb);

        r = Converter.Exec(mrb, "CodeGenSample.new(1,'2')");

        Debug.Log(Converter.ToString(mrb, r));

        Debug.Log(Converter.ToString(mrb, Converter.Send(mrb, r, "GetIntValue")));
    }
}

