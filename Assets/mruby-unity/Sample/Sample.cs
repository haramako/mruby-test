using System.Linq;
using MRuby;

[CustomMRubyClass]
public class Sample
{
    public int IntField;
    public string StringField;

    public int IntProperty { get; set; }
    public string StringProperty { get; set; }

    public Sample(int i, string s)
    {
        IntField = i;
        StringField = s;
    }

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
        return string.Join(",", ary);
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

    public int OverloadedMethod2()
    {
        return 0;
    }

    public int OverloadedMethod2(int n)
    {
        return n;
    }

    public long IntTypes(byte v1 = 0, short v2 = 1, ushort v3 = 2, int v4 = 3, uint v5 = 4, long v6 = 5, ulong v7 = 6, char v8 = (char)7)
    {
        return v1 + v2 + v3 + v4 + v5 + v6 + (long)v7 + (int)v8;
    }

    public string BoolTypes(bool v1 = true, bool v2 = true, bool v3 = false, bool v4 = false)
    {
        return $"{v1},{v2},{v3},{v4}";
    }


    public enum TestEnum { A, B, C }

    public string EnumTypes(TestEnum v1, TestEnum v2 = TestEnum.B)
    {
        return $"{v1},{v2}";
    }

    public TestEnum EnumResult(int n)
    {
        return (TestEnum)n;
    }
}

[CustomMRubyClass]
public class DerivedClass : BaseClass
{
    public int B() => 2;
    public override int Virtual() => 2;
}

[CustomMRubyClass]
public class BaseClass
{
    public int A() => 1;
    public virtual int Virtual() => 1;
}

[CustomMRubyClass]
public class ClassInClass
{
    [CustomMRubyClass]
    public class ClassInClassChild
    {
        public ClassInClassChild() { }
        public int Num() => 99;
    }

    public int Num() => 1;
}

namespace NSSample
{
    [CustomMRubyClass]
    public class NSClass
    {
        public NSClass()
        {
        }
        public int Func() => 1;
    }
}


[CustomMRubyClass]
public static class ExtTest
{
    public static void ExSet(this Extended self, int i) { }
    public static int Set(this Extended self, int i) => i;
    //public static T Set<T>(this Extended self, T i) => i;
}

[CustomMRubyClass]
public class Extended
{
    //public Extended() { }

    //public void Set(int i) { }
    //public void Set(string s) { }
    //public void Set<T>(T v) { }
}

class GenericTest
{

}
