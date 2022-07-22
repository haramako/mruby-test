﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MRuby.CodeGen
{
    public class CodeGenUtil
    {
        public static bool Generate(Type t, string ns, string path)
        {
            var registry = new Registry();
            CodeGenerator cg = new CodeGenerator();
            cg.givenNamespace = ns;
            cg.path = path;
            var ok = cg.Generate(t, registry);

            var nsGen = new NamespaceGen(registry, "../CodeGenTest/AutoGenerated/");
            nsGen.Generate();

            return ok;
        }
    }
}
