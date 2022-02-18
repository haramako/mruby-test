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

        Debug.Log(new Value(mrb, "hoge").AsString(mrb));

        var r = mrb.LoadString("(2+10).to_s");

        Debug.Log(r.AsString(mrb));

        Debug.Log(r.ToString(mrb));

        var r2 = r.Send(mrb, "to_i");

        Debug.Log(r2.AsInteger(mrb));

        Debug.Log(new Value(99).Send(mrb, "to_s", 8).ToString(mrb));


        Character_CSObject.RegisterClass(mrb);

        var r3 = mrb.LoadString("Character.new('a',1).show");

        Debug.Log(r3.AsString(mrb));


    }
}

