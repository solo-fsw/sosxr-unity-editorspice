using UnityEditor;
using UnityEditor.Compilation;


/// <summary>
///     From: https://github.com/adammyhre/Unity-Utils
/// </summary>
public static class CompileProject
{
    [MenuItem("File/Compile _F5")]
    private static void Compile()
    {
        CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
    }
}