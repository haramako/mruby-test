using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRuby;
using System.Linq;
using DG.Tweening;
using UnityEngine.U2D;
using UnityEngine.UI;

[CustomMRubyClass]
public class Command
{
    public readonly string Type;
    public int Card;
    public int MoveTo;

    public Command(string t)
    {
        Type = t;
    }

    public override string ToString()
    {
        return $"{Type} {Card}";
    }

    public string Inspect()
    {
        return ToString();
    }
}

public class MainScene : MonoSingleton<MainScene>
{
    public BoardView View;
    public SpriteAtlas CardAtlas;
    public Button ResetButton;
    public Button StartButton;
    public Button DiscardButton;
    public Button BetButton;
    public Canvas Canvas;

    VM mrb;

    public Value Game { get; private set; }

    public VM Mrb => mrb;

    void Start()
    {
        //testBoard();
        run();
    }

    private void Update()
    {
        if( Input.GetKeyDown(KeyCode.F5))
        {
            run();
        }
    }

    void run()
    {
        View.Clear();

        var opt = new VMOption()
        {
            LoadPath = new string[] { "../tcg2" },
        };
        mrb = new VM(opt);

        Value r;

        mrb.Run("require 'app'");

        r = mrb.Run("Dummy::DummyRule.new");
        //r = mrb.Run("PokerRule.new");
        Game = r;

        Play(new Command("reset"));
        Play(new Command("start"));
    }

    void testBoard()
    {
        var c1 = (Card)View.Create("Card", 3);
        c1.MoveTo(100, 100, 1.0f);
        //c1.transform.localPosition = new Vector3(100, -100, 0);

        //c1.transform.DOLocalMoveX(200, 1.0f).SetEase(Ease.OutBounce);
    }

    public Value Play(Command cmd)
    {
        var result = Game.x("play", cmd);
        Game.x("board").x("root").x("redraw_all", View);
        redraw();
        return result;
    }

    void redraw()
    {
        ResetButton.interactable = Game.x("trigger?", "reset").AsBool();
        StartButton.interactable = Game.x("trigger?", "start").AsBool();
        DiscardButton.interactable = Game.x("trigger?", "discard").AsBool();
        BetButton.interactable = Game.x("trigger?", "bet").AsBool();
    }

    public void OnResetClick()
    {
        Play(new Command("reset"));
    }

    public void OnStartClick()
    {
        Play(new Command("start"));
    }

    public void OnDiscardClick()
    {
        Play(new Command("discard"));
    }

    public void OnBetClick()
    {
        var r = Play(new Command("bet"));
        Debug.Log(r.ToString());
    }

}
