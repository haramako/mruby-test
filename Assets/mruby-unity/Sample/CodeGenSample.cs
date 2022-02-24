using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[MRuby.CustomMRubyClass]
public class CodeGenSample
{
    string s;
    int i;

    public int IntField;
    public string StringField;

    public int IntProperty { get; set; }
    public string StringProperty { get; set; }

    public string GetStringValue()
    {
        return s;
    }

    public int GetIntValue()
    {
        return i;
    }
}
