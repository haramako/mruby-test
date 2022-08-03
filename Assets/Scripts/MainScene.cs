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

    VM mrb;

    public Value Poker;

    void Start()
    {
        //testBoard();
        StartCoroutine(testRuby());
    }

    IEnumerator testRuby()
    {
        var opt = new VMOption()
        {
            LoadPath = new string[] { "../tcg2" },
        };
        mrb = new VM(opt);

        Value r;

        mrb.Run("require 'app'");

        r = mrb.Run("PokerRule.new");
        Poker = r;

        Play(new Command("start"));
        yield return new WaitForSeconds(0.5f);
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
        Poker.x("play", cmd);
        Poker.x("board").x("root").x("redraw_all", View);
    }

}
