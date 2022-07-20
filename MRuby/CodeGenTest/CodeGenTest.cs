using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using MRuby;

class CodeGenTest
{
    MrbState mrb;

    [SetUp]
    public void Setup()
    {
        mrb = new MrbState();
    }

    [TestCase("1+1", "2")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').IntField", "1")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').StringField", "a")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').getstringvalue", "str")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').getintvalue()", "99")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').overloadedmethod(1)", "1")]
    [TestCase("Hoge::CodeGenSample.staticmethod(2)", "2")]
    public void TestSample(string src, string expect)
    {
        MRuby_Hoge_CodeGenSample.reg(mrb.mrb);

        var r = mrb.LoadString(src);
        var rstr = r.ToString(mrb);
        Assert.AreEqual(expect, rstr);
    }

}

