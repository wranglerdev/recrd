namespace Recrd.Core.Interfaces;

public interface ITestCompiler
{
    string TargetName { get; }
    Task<CompilationResult> CompileAsync(Ast.Session session, CompilerOptions options, CancellationToken cancellationToken = default);
}

public sealed record CompilerOptions(
    string OutputDirectory = "./output",
    string SelectorStrategy = "data-testid,id,css,xpath",
    int TimeoutSeconds = 10,
    bool EnableNetworkInterception = false
);

public sealed record GeneratedFile(string RelativePath, string Content);

public sealed record CompilationResult(
    IReadOnlyList<GeneratedFile> Files,
    IReadOnlyList<string> Warnings
);
