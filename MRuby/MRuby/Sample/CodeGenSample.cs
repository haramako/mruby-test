using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRuby;
using System.IO;

namespace Hoge
{
    [MRuby.CustomMRubyClass]
    public class CodeGenSample
    {
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
            return "str";
        }

        public int GetIntValue()
        {
            return 99;
        }

        public int OverloadedMethod(int n)
        {
            return n;
        }

        public static int StaticMethod(int n)
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

    [MRuby.CustomMRubyClass]
    public class BaseClass
    {
        public int A() => 1;
    }

    [MRuby.CustomMRubyClass]
    public class DerivedClass : BaseClass
    {
        public int B() => 2;
    }

}