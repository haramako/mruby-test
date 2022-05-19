using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRuby;
using System.Linq;
using DG.Tweening;

public class MainScene : MonoSingleton<MainScene>
{
    public Board Board;

    MrbState mrbState;

    void Start()
    {
        //testBoard();
        testRuby();
    }

    void testRuby()
    {
        mrbState = new MrbState();
        var mrb = mrbState.mrb;

        MRubySvr svr = new MRubySvr(mrbState);
        MRubySvr.doBind(mrb);

        using var arena = Converter.LockArena(mrb);

        Value r;

        //MRuby_MRubyUnity_Console.reg(_mrb);

        MRubyUnity.Core.Require(mrb, "prelude");
        MRubyUnity.Core.LoadPath = MRubyUnity.Core.LoadPath.Concat(new string[] { "../tcg2" }).ToArray();
        MRubyUnity.Core.Require(mrb, "app");

        r = new Value(DLL.mrb_load_string(mrb, "EntryPoint"));

        r = r.Send(mrbState, "run", 1);

        Debug.Log(r.ToString(mrbState));
    }

    void testBoard()
    {
        var c1 = (Card)Board.Create("Card", 1);
        c1.MoveTo(100, 100, 1.0f);
        //c1.transform.localPosition = new Vector3(100, -100, 0);

        //c1.transform.DOLocalMoveX(200, 1.0f).SetEase(Ease.OutBounce);
    }

}
