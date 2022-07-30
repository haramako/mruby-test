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
        Assert.AreEqual(expect, Naming.ToSnakeCase(input));
    }

    [TestCase("1+1", "2")]
    [TestCase("Sample.new.int_field", "1")]
    [TestCase("Sample.new.string_field", "a")]
    [TestCase("Sample.new.get_string_value", "str")]
    [TestCase("Sample.new.get_int_value", "99")]
    [TestCase("Sample.new.overloaded_method(1)", "1")]
    [TestCase("Sample.static_method(2)", "2")]
    public void TestSample2(string src, string expect)
    {
        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }

    [TestCase("Sample.new.get_int_value(1)", "wrong number of arguments (given 1, expected 0)")] // Over argument
    [TestCase("Sample.new.overloaded_method()", "wrong number of arguments (given 0, expected 1)")] // Less argument
    [TestCase("Sample.new.overloaded_method(1,2)", "wrong number of arguments (given 2, expected 1)")] // Over argument
    [TestCase("Sample.new.overloaded_method('a')", "String cannot be converted to Integer")] // Invalid type of argument
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


    [TestCase("DerivedClass.new.a", "1")]
    [TestCase("DerivedClass.new.b", "2")]
    [TestCase("DerivedClass.new.virtual", "2")]
    [TestCase("BaseClass.new.a", "1")]
    [TestCase("BaseClass.new.virtual", "1")]
    public void TestInheritance(string src, string expect)
    {
        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }

    [TestCase("ClassInClass::ClassInClassChild.new.num", "99")]
    [TestCase("ClassInClass.new.num", "1")]
    public void TestClassInClass(string src, string expect)
    {
        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }

    [TestCase("Sample.new.with_default_value(1)", "1,2,def")]
    [TestCase("Sample.new.with_default_value(1,99)", "1,99,def")] 
    [TestCase("Sample.new.with_default_value(1,99,\"hoge\")", "1,99,hoge")] 
    public void TestDefaultParameters(string src, string expect)
    {
        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }

    [TestCase("Sample.new.with_default_value()")] // Less argument
    [TestCase("Sample.new.with_default_value(1,\"a\")")] // Invalid type
    [TestCase("Sample.new.with_default_value(1,99,\"hoge\",3)")] // Over argument
    public void TestDefaultParameterErrors(string src)
    {
        Assert.Throws<RubyException>(() => mrb.LoadString(src));
    }


    [TestCase("NSSample::NSClass.new.func", "1")]
    public void TestNamespace(string src, string expect)
    {
        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }

    [TestCase("Sample.new.int_array([1,2,3])", "6")]
    [TestCase("Sample.new.str_array(['1','2','3'])", "1,2,3")]
    [TestCase("Sample.new.int_array_result(3)", "System.Int32[]")]
    [TestCase("Sample.new.str_array_result(3)", "System.String[]")]
    [TestCase("Sample.new.str_array_result(3) .length", "3")]
    public void TestArray(string src, string expect)
    {
        Assert.AreEqual(expect, mrb.LoadString(src).ToString());
    }
}

