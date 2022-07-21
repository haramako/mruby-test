using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using MRuby;
using MRuby.CodeGen;

class ValueTest
{
    MrbState mrb;

    [SetUp]
    public void Setup()
    {
        mrb = new MrbState();
    }

    [Test]
    public void TestLoadString()
    {
        var v = mrb.LoadString("1+1");
        Assert.AreEqual(2, v.AsInteger());
    }

    [Test]
    public void TestSend()
    {
        var v = new Value(mrb, 1);
        Assert.AreEqual(2, v.Send("+", 1).AsInteger());
        Assert.AreEqual(2, v.Send("succ").AsInteger());
        Assert.Throws<Exception>(() => v.Send("invalid"));
    }

    [Test]
    public void TestAsString()
    {
        Assert.AreEqual("hoge", new Value(mrb, "hoge").AsString());
        Assert.Throws<Exception>(() => new Value(mrb, 1).AsString());
    }


    [Test]
    public void TestAsInteger()
    {
        Assert.AreEqual(1, new Value(mrb, 1).AsInteger());
        Assert.Throws<Exception>(() => new Value(mrb, "hoge").AsInteger());
    }

    [Test]
    public void TestToInteger()
    {
        Assert.AreEqual(1, new Value(mrb, 1).ToInteger());
        Assert.AreEqual(1, new Value(mrb, "1").ToInteger());
        Assert.AreEqual(0, new Value(mrb, "hoge").ToInteger());
    }

}
