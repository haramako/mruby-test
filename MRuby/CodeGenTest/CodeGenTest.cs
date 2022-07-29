using System;
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
        _Binder.Bind(mrb.mrb);
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
    [TestCase("Hoge::CodeGenSample.new.int_field", "1")]
    [TestCase("Hoge::CodeGenSample.new.string_field", "a")]
    [TestCase("Hoge::CodeGenSample.new.get_string_value", "str")]
    [TestCase("Hoge::CodeGenSample.new.get_int_value", "99")]
    [TestCase("Hoge::CodeGenSample.new.overloaded_method(1)", "1")]
    [TestCase("Hoge::CodeGenSample.static_method(2)", "2")]
    public void TestSample2(string src, string expect)
    {
        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }

    [TestCase("Hoge::CodeGenSample.new.get_int_value(1)", "wrong number of arguments (given 1, expected 0)")] // Over argument
    [TestCase("Hoge::CodeGenSample.new.overloaded_method()", "wrong number of arguments")] // Less argument
    [TestCase("Hoge::CodeGenSample.new.overloaded_method(1,2)", "wrong number of arguments (given 2, expected 1)")] // Over argument
    [TestCase("Hoge::CodeGenSample.new.overloaded_method('a')", "String cannot be converted to Integer")] // Invalid type of argument
    public void TestArgumentError(string src, string errorMessage)
    {
        try
        {
            mrb.LoadString(src);
        }
        catch (Exception ex)
        {
            Assert.AreEqual(errorMessage, ex.Message);
            return;
        }
        Assert.Fail();
    }


    [TestCase("Hoge::DerivedClass.new.a", "1")]
    [TestCase("Hoge::DerivedClass.new.b", "2")]
    [TestCase("Hoge::DerivedClass.new.virtual", "2")]
    [TestCase("Hoge::BaseClass.new.a", "1")]
    [TestCase("Hoge::BaseClass.new.virtual", "1")]
    public void TestInheritance(string src, string expect)
    {
        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }

    [TestCase("Hoge::ClassInClass::ClassInClassChild.new.num", "99")]
    [TestCase("Hoge::ClassInClass.new.num", "1")]
    public void TestClassInClass(string src, string expect)
    {
        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }

}

