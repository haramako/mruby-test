using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRuby;
using System.Linq;
using DG.Tweening;

public class MainScene : MonoSingleton<MainScene>
{
    public BoardView View;

    MrbState mrbState;

    void Start()
    {
        //testBoard();
        StartCoroutine(testRuby());
    }

    IEnumerator testRuby()
    {
        mrbState = new MrbState();
        var mrb = mrbState.mrb;

        using var arena = Converter.LockArena(mrb);

        Value r;

        //MRuby_MRubyUnity_Console.reg(_mrb);

        MRubyUnity.Core.LoadPath = MRubyUnity.Core.LoadPath.Concat(new string[] { "../tcg2" }).ToArray();
        MRubyUnity.Core.Require(mrb, "app");

        r = new Value(DLL.mrb_load_string(mrb, "EntryPoint"));

        r = r.Send(mrbState, "run", View);

        //Debug.Log(r.ToString(mrbState));

        yield return new WaitForSeconds(1.0f);

        //using (var arena = Converter.ArenaLock(mrb))
        {
            var h = new Value(mrb, DLL.mrb_obj_new(mrb, DLL.mrb_class_get(mrb, "Hash"), 0, null));
            h.Send(mrbState, "[]=", DLL.mrb_symbol_value(DLL.mrb_intern_cstr(mrb, "type")), DLL.mrb_symbol_value(DLL.mrb_intern_cstr(mrb, "select")));
            h.Send(mrbState, "[]=", DLL.mrb_symbol_value(DLL.mrb_intern_cstr(mrb, "card")), 5);

            r.Send(mrbState, "play", h);


            h = new Value(mrb, DLL.mrb_obj_new(mrb, DLL.mrb_class_get(mrb, "Hash"), 0, null));
            h.Send(mrbState, "[]=", DLL.mrb_symbol_value(DLL.mrb_intern_cstr(mrb, "type")), DLL.mrb_symbol_value(DLL.mrb_intern_cstr(mrb, "discard")));

            r.Send(mrbState, "play", h);


            r = r.Send(mrbState, "board").Send(mrbState, "root").Send(mrbState, "redraw_all", View);


            Debug.Log(r.ToString(mrbState));
        }


        //r.Send("app", );

    }

    void testBoard()
    {
        var c1 = (Card)View.Create("Card", 3);
        c1.MoveTo(100, 100, 1.0f);
        //c1.transform.localPosition = new Vector3(100, -100, 0);

        //c1.transform.DOLocalMoveX(200, 1.0f).SetEase(Ease.OutBounce);
    }

}
