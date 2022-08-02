using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRuby;
using System.Linq;
using DG.Tweening;


[CustomMRubyClass]
public class Command
{
    public readonly string Type;
    public int Card;

    public Command(string t)
    {
        Type = t;
    }
}

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
        Binder.Bind(mrb, _Binder.BindData);
        mrb.LoadString(MrbState.prelude);

        using var arena = Converter.LockArena(_mrb);

        Value r;

        //MRuby_MRubyUnity_Console.reg(_mrb);

        MRubyUnity.Core.LoadPath = MRubyUnity.Core.LoadPath.Concat(new string[] { "../tcg2" }).ToArray();
        MRubyUnity.Core.Require(_mrb, "app");

        r = new Value(_mrb, DLL.mrb_load_string(_mrb, "EntryPoint"));

        Debug.Log(r.ToString());

        r = r.Send("run", View);

        //Debug.Log(r.ToString(mrbState));

        yield return new WaitForSeconds(1.0f);

        //using (var arena = Converter.ArenaLock(mrb))
        {
            r.Send("play", new Command("select") { Card = 5 });
            r.Send("play", new Command("discard"));


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
