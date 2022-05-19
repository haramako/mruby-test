using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MRuby;

[CustomMRubyClass]
public class Card : BoardObject
{
    public Text NameText;

    public void Redraw(string name)
    {
        NameText.text = name;
    }
}

