using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MRuby;
using UnityEngine.EventSystems;

[CustomMRubyClass]
public class PlaceHolder : BoardObject
{
    public Text NameText;
    public Button Button;
    public Image Image;
    //public Image CardImage;

    public void Redraw(string name, bool selected, bool reversed)
    {
        NameText.text = name;
        //Image.color = selected ? new Color(1.0f, 0.7f, 0.7f) : Color.white;
    }

    public void OnClick()
    {
        var selectable = MainScene.Instance.Game.x("selectable?", ObjectID).AsBool();
        if (selectable)
        {
            MainScene.Instance.Play(new Command("select") { Card = ObjectID });
        }
    }

}
