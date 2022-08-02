using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRuby;
using System.Linq;
using DG.Tweening;

public class MainScene : MonoSingleton<MainScene>
{
    public BoardView View;

    MrbState mrb;

    void Start()
    {
        //testBoard();
        StartCoroutine(testRuby());
    }

    IEnumerator testRuby()
    {
        mrb = new MrbState();
        var _mrb = mrb.mrb;

        using var arena = Converter.LockArena(_mrb);

        Value r;

        //MRuby_MRubyUnity_Console.reg(_mrb);

        MRubyUnity.Core.LoadPath = MRubyUnity.Core.LoadPath.Concat(new string[] { "../tcg2" }).ToArray();
        MRubyUnity.Core.Require(_mrb, "app");

        r = new Value(DLL.mrb_load_string(_mrb, "EntryPoint"));

        r = r.Send("run", View);

        //Debug.Log(r.ToString(mrbState));

        yield return new WaitForSeconds(1.0f);

        //using (var arena = Converter.ArenaLock(mrb))
        {
            var h = new Value(_mrb, DLL.mrb_obj_new(_mrb, DLL.mrb_class_get(_mrb, "Hash"), 0, null));
            h.Send("[]=", DLL.mrb_symbol_value(DLL.mrb_intern_cstr(_mrb, "type")), DLL.mrb_symbol_value(DLL.mrb_intern_cstr(_mrb, "select")));
            h.Send("[]=", DLL.mrb_symbol_value(DLL.mrb_intern_cstr(_mrb, "card")), 5);

            r.Send("play", h);


            h = new Value(_mrb, DLL.mrb_obj_new(_mrb, DLL.mrb_class_get(_mrb, "Hash"), 0, null));
            h.Send("[]=", DLL.mrb_symbol_value(DLL.mrb_intern_cstr(_mrb, "type")), DLL.mrb_symbol_value(DLL.mrb_intern_cstr(_mrb, "discard")));

            r.Send("play", h);


            r = r.Send("board").Send("root").Send("redraw_all", View);


            //Debug.Log(r.ToString(mrbState));
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
