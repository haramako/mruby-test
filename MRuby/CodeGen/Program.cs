using System;
using MRuby;

namespace CodeGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            DLL.mrb_open();
        }
    }
}
