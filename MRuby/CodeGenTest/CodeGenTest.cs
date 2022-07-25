﻿using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using MRuby;
using MRuby.CodeGen;

class CodeGenTest
{
    MrbState mrb;

    [SetUp]
    public void Setup()
    {
        mrb = new MrbState();
    }

    [TestCase("hoge", "hoge")]
    [TestCase("A", "a")]
    [TestCase("Hoge", "hoge")]
    [TestCase("HogeFuga", "hoge_fuga")]
    [TestCase("HogeFUGA", "hoge_fuga")]
    public void TestRubyMethodName(string input, string expect)
    {
        Assert.AreEqual(expect, CodeGenerator.RubyMethodName(input));
    }

    [TestCase("1+1", "2")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').int_field", "1")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').string_field", "a")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').get_string_value", "str")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').get_int_value()", "99")]
    [TestCase("Hoge::CodeGenSample.new(1,'a').overloaded_method(1)", "1")]
    [TestCase("Hoge::CodeGenSample.static_method(2)", "2")]
    public void TestSample2(string src, string expect)
    {
        _Binder.RegisterNamespaces(mrb.mrb);
        MRuby_Hoge_CodeGenSample.RegisterMembers(mrb.mrb);
        MRuby_Hoge_DerivedClass.RegisterMembers(mrb.mrb);
        MRuby_Hoge_BaseClass.RegisterMembers(mrb.mrb);

        var r = mrb.LoadString(src);
        var rstr = r.ToString();
        Assert.AreEqual(expect, rstr);
    }


    [TestCase("Hoge::DerivedClass.new.a", "1")]
    [TestCase("Hoge::DerivedClass.new.b", "2")]
    [TestCase("Hoge::DerivedClass.new.virtual", "2")]
    [TestCase("Hoge::BaseClass.new.a", "1")]
    [TestCase("Hoge::BaseClass.new.virtual", "1")]
    public void TestInheritance(string src, string expect)
    {
        _Binder.RegisterNamespaces(mrb.mrb);
        MRuby_Hoge_CodeGenSample.RegisterMembers(mrb.mrb);
        MRuby_Hoge_DerivedClass.RegisterMembers(mrb.mrb);
        MRuby_Hoge_BaseClass.RegisterMembers(mrb.mrb);

        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }

}

