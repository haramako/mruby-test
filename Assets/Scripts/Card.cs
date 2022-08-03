using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MRuby;

[CustomMRubyClass]
public class Card : BoardObject
{
    public Text NameText;
    public Button Button;
    public Image Image;

    public void Redraw(string name, int selected)
    {
        NameText.text = name;
        Image.color = (selected != 0) ? new Color(1.0f, 0.7f, 0.7f) : Color.white;
    }

    public void OnClick()
    {
        MainScene.Instance.Play(new Command("select") { Card = ObjectID });
    }
}
