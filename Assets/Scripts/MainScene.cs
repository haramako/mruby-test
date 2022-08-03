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

    public override string ToString()
    {
        return $"{Type} {Card}";
    }
}

public class MainScene : MonoSingleton<MainScene>
{
    public BoardView View;

    MrbState mrb;

    public Value Poker;

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

        Value r;

        MRubyUnity.Core.LoadPath = MRubyUnity.Core.LoadPath.Concat(new string[] { "../tcg2" }).ToArray();
        MRubyUnity.Core.Require(_mrb, "app");

        r = mrb.LoadString("PokerRule.new");
        Poker = r;

        r.Send("board").Send("root").Send("redraw_all", View);
        yield return new WaitForSeconds(1.0f);

        r.Send("play", new Command("start"));
#if false
        r.Send("play", new Command("select") { Card = 5 });
        r.Send("play", new Command("discard"));

        r.Send("board").Send("root").Send("redraw_all", View);
        yield return new WaitForSeconds(1.0f);

        r.Send("play", new Command("select") { Card = 6 });
        r.Send("play", new Command("discard"));

        r.Send("board").Send("root").Send("redraw_all", View);
#endif
        r.Send("board").Send("root").Send("redraw_all", View);
        yield return new WaitForSeconds(1.0f);
    }

    void testBoard()
    {
        var c1 = (Card)View.Create("Card", 3);
        c1.MoveTo(100, 100, 1.0f);
        //c1.transform.localPosition = new Vector3(100, -100, 0);

        //c1.transform.DOLocalMoveX(200, 1.0f).SetEase(Ease.OutBounce);
    }

    public void OnStartClick()
    {
        Play(new Command("start"));
    }

    public void OnDiscardClick()
    {
        Play(new Command("discard"));
    }

    public void Play(Command cmd)
    {
        Poker.Send("play", cmd);
        Poker.Send("board").Send("root").Send("redraw_all", View);
    }

}
