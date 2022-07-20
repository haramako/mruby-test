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
    public void TestA()
    {
        var v = mrb.LoadString("1+1");
        Assert.AreEqual(2, v.AsInteger());
        Assert.AreEqual("2", v.ToString());
    }

}
