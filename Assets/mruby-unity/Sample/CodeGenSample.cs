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

    public CodeGenSample(int i, string s)
    {
        IntField = i;
        StringField = s;
    }

    public string GetStringValue()
    {
        return s;
    }

    public int GetIntValue()
    {
        return i;
    }

    public int OverloadedMethod(int n)
    {
        return n;
    }

#if false
    public int OverloadedMethod(int n, int m)
    {
        return n + m;
    }

    public string OverloadedMethod(string s)
    {
        return s;
    }
#endif

}
