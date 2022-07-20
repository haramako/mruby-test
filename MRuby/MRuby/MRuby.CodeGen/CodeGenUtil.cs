using System;
using System.Collections.Generic;
using System.Text;

namespace MRuby.CodeGen
{
    public class CodeGenUtil
    {
        public static bool Generate(Type t, string ns, string path)
        {
            CodeGenerator cg = new CodeGenerator();
            cg.givenNamespace = ns;
            cg.path = path;
            return cg.Generate(t);
        }
    }
}
