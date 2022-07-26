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
        Assert.AreEqual(2, v.Send("+", new Value(mrb, 1)).AsInteger());
        Assert.AreEqual(2, v.Send("succ").AsInteger());
        Assert.Throws<RubyException>(() => v.Send("invalid"));
    }

    [Test]
    public void TestSendAfterError()
    {
        var v = new Value(mrb, 1);
        Assert.Throws<RubyException>(() => v.Send("invalid"));
        Assert.AreEqual("1", v.Send("to_s").ToString());
        Assert.Throws<RubyException>(() => v.Send("invalid", 0));
        Assert.AreEqual("1", v.Send("to_s").ToString());
        Assert.Throws<RubyException>(() => v.Send("invalid", new Value(mrb, 0)));
        Assert.AreEqual("1", v.Send("to_s").ToString());
    }

    [Test]
    public void TestAsString()
    {
        Assert.AreEqual("hoge", new Value(mrb, "hoge").AsString());
        Assert.Throws<AbortException>(() => new Value(mrb, 1).AsString());
    }


    [Test]
    public void TestAsInteger()
    {
        Assert.AreEqual(1, new Value(mrb, 1).AsInteger());
        Assert.Throws<AbortException>(() => new Value(mrb, "hoge").AsInteger());
    }

    [Test]
    public void TestToInteger()
    {
        Assert.AreEqual(1, new Value(mrb, 1).ToInteger());
        Assert.AreEqual(1, new Value(mrb, "1").ToInteger());
        Assert.AreEqual(0, new Value(mrb, "hoge").ToInteger());
    }   

}
