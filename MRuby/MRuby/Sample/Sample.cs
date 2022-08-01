using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRuby;
using System.IO;
using System.Linq;

[MRuby.CustomMRubyClass]
public class Sample
{
    public int IntField;
    public string StringField;

    public int IntProperty { get; set; }
    public string StringProperty { get; set; }

    public Sample()
    {
        IntField = 1;
        StringField = "a";
    }

#if false
        public CodeGenSample(int i, string s)
        {
            IntField = i;
            StringField = s;
        }
#endif

    public string GetStringValue()
    {
        return "str";
    }

    public int GetIntValue()
    {
        return 99;
    }

    public void SetIntValue(int n)
    {
        IntField = n;
    }

    public static int StaticMethod(int n)
    {
        return n;
    }

    public string WithDefaultValue(int n, int m = 2, string s = "def")
    {
        return $"{n},{m},{s}";
    }

    public int IntArray(int[] ary)
    {
        return ary.Sum();
    }

    public string StrArray(string[] ary)
    {
        return string.Join(',', ary);
    }

    public int[] IntArrayResult(int n)
    {
        return Enumerable.Repeat<int>(1, n).ToArray();
    }

    public string[] StrArrayResult(int n)
    {
        return Enumerable.Repeat<string>("a", n).ToArray();
    }

    public int OverloadedMethod(int n)
    {
        return n;
    }

    public int OverloadedMethod(int n, int m)
    {
        return n + m;
    }

    public string OverloadedMethod(string s)
    {
        return s + "*";
    }

}

[MRuby.CustomMRubyClass]
public class DerivedClass : BaseClass
{
    public int B() => 2;
    public override int Virtual() => 2;
}

[MRuby.CustomMRubyClass]
public class BaseClass
{
    public int A() => 1;
    public virtual int Virtual() => 1;
}

[MRuby.CustomMRubyClass]
public class ClassInClass
{
    [MRuby.CustomMRubyClass]
    public class ClassInClassChild
    {
        public ClassInClassChild() { }
        public int Num() => 99;
    }

    public int Num() => 1;
}

namespace NSSample
{
    [MRuby.CustomMRubyClass]
    public class NSClass
    {
        public NSClass()
        {
        }
        public int Func() => 1;
    }
}


[MRuby.CustomMRubyClass]
public static class ExtTest
{
    public static void ExSet(this Extended self, int i) { }
    public static void Set(this Extended self, int i) { }
    public static void Set<T>(this Extended self, T i) { }
}

[MRuby.CustomMRubyClass]
public class Extended
{
    //public Extended() { }

    public void Set(int i) { }
    public void Set(string s) { }
    //public void Set<T>(T v) { }
}

class GenericTest
{

}