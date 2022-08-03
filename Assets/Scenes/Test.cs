using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MRuby;
using System.IO;
using System.Linq;

public class Test : MonoBehaviour
{
    public void OnButtonClick()
    {
        VM _mrb = new VM();
        var mrb = _mrb.mrb;

        using (var arena = Converter.LockArena(mrb))
        {

            //mrb_value r;

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

            MRubyUnity.Core.LoadPath = MRubyUnity.Core.LoadPath.Concat(new string[] { "../tcg2" }).ToArray();
            MRubyUnity.Core.Require(mrb, "main");
        }
    }
}

