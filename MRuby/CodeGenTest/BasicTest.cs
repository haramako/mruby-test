using NUnit.Framework;
using MRuby;
using System.Linq;

public class BasicTest
{
    [Test]
    public void TestDLL()
    {
        MrbState _mrb = new MrbState();
        var mrb = _mrb.mrb;

        using (var arena = Converter.LockArena(mrb))
        {
            var r = DLL.mrb_load_string(mrb, "1+1");
            var n = DLL.mrb_as_int(mrb, r);
            Assert.AreEqual(2, n);
        }
    }

    [Test]
    public void TestMrbState()
    {
        MrbState mrb = new MrbState();

        using (var arena = Converter.LockArena(mrb.mrb))
        {
            var r = mrb.LoadString("1+1").ToString();
            Assert.AreEqual("2", r);
        }
    }
}
