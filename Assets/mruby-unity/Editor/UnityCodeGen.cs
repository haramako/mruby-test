using UnityEditor;
using MRuby.CodeGen;

[InitializeOnLoad]
public class UnityCodeGen
{
    [MenuItem("MRuby/Generate Code")]
    static public void GenerateCode()
    {
        var opt = new MRubyCodeGen.Option
        {
            OutputDir = "Assets/mruby-unity/AutoGenerated/",
        };
        MRubyCodeGen.Run(opt);

        AssetDatabase.Refresh();
    }
}
