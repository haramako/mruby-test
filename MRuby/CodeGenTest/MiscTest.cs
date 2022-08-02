using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using MRuby.CodeGen;

class MiscTest
{
    void method(Type t, string name)
    {

    }

    [TestCase("hoge", "hoge")]
    [TestCase("A", "a")]
    [TestCase("Hoge", "hoge")]
    [TestCase("HogeFuga", "hoge_fuga")]
    [TestCase("HogeFUGA", "hoge_fuga")]
    [TestCase("_HogeFUGA", "_hoge_fuga")]
    public void TestRubyMethodName(string input, string expect)
    {
        Assert.AreEqual(expect, Naming.ToSnakeCase(input));
    }

    [Test]
    public void TestIsExtensionMethod()
    {
        var method = typeof(ExtTest).GetMethod("ExSet");
        Assert.AreEqual(true, TypeUtil.IsExtensionMethod(method));
        Assert.AreEqual(typeof(Extended), TypeUtil.ExtensionTargetClass(method));
    }

}
