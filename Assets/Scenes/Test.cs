using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MRuby;

public class Test : MonoBehaviour
{
    public void OnButtonClick()
    {
        MrbState mrb = new MrbState();
#if false
        MRuby_Character.RegisterClass(mrb);

        var r = mrb.LoadString("2+10");

        Debug.Log(r.ToString(mrb));

        var r3 = mrb.LoadString("Character.new('a',1).show");

        Debug.Log(r3.AsString(mrb));

        var r4 = new Value(mrb, new Character("hoge", 3));

        Debug.Log(r4.Send(mrb, "show").ToString(mrb));
#endif

        Converter.sym_objid = DLL.mrb_intern_cstr(mrb.mrb, "objid");
        MRuby_CodeGenSample.reg(mrb);

        var r5 = mrb.LoadString("CodeGenSample");
        Debug.Log(r5.ToString(mrb));

        //var r = r5.Send(mrb, "methods");
        var r = r5.Send(mrb, "new", 1, "2");
        Debug.Log(r.ToString(mrb));
        Debug.Log(r.Send(mrb, "GetIntValue").ToString(mrb));

    }
}

