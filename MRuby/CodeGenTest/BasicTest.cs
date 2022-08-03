using NUnit.Framework;
using MRuby;
using System.Linq;
using System;

public class BasicTest
{
    [Test]
    public void TestDLL()
    {
        VM _mrb = new VM();
        var mrb = _mrb.mrb;

        using (var arena = Converter.LockArena(mrb))
        {
            var r = DLL.mrb_load_string(mrb, "1+1");
            var n = DLL.mrb_as_int(mrb, r);
            Assert.AreEqual(2, n);
        }
    }

    [Test]
    public void TestMrbLoadStringWithError()
    {
        VM _mrb = new VM();
        var mrb = _mrb.mrb;

        using (var arena = Converter.LockArena(mrb))
        {
            var r = DLL.mrb_load_string(mrb, "hoge");
            Assert.AreEqual(true, r.IsNil); // mrb_load_string() returns nil when error occured.
            Assert.AreEqual(false, DLL.mrb_mrb_state_exc(mrb).IsNil); // mrb->exc is set when error occured.
        }
    }

    [Test]
    public void TestAbortCallback()
    {
        VM _mrb = new VM();
        var mrb = _mrb.mrb;

        using (var arena = Converter.LockArena(mrb))
        {
            var r = DLL.mrb_load_string(mrb, "1");
            Assert.Throws<AbortException>(() => DLL.mrb_as_string(mrb, r));
        }
    }

    [Test]
    public void TestMrbState()
    {
        VM mrb = new VM();

        using (var arena = Converter.LockArena(mrb.mrb))
        {
            var r = mrb.LoadString("1+1").ToString();
            Assert.AreEqual("2", r);
        }
    }

    [Test]
    public void TestStacktrace()
    {
        VM mrb = new VM();

        using (var arena = Converter.LockArena(mrb.mrb))
        {
            try
            {
                mrb.LoadString(@"
def rec(n)
  if n <= 0 then raise 'ERROR' else rec(n-1) end
end
rec(10)
");
            }
            catch (RubyException ex)
            {
                Assert.AreEqual("ERROR", ex.Message);
                Assert.AreEqual(12, ex.StackTrace.Split("\n").Length);
            }
        }
    }

}
