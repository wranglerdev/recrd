namespace Recrd.Core.Interfaces;

public sealed record CompilationResult
{
    public IReadOnlyList<string> GeneratedFiles { get; }
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyDictionary<string, string> DependencyManifest { get; }

    public CompilationResult(
        IReadOnlyList<string> generatedFiles,
        IReadOnlyList<string> warnings,
        IReadOnlyDictionary<string, string> dependencyManifest)
    {
        GeneratedFiles = generatedFiles ?? throw new ArgumentNullException(nameof(generatedFiles));
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        DependencyManifest = dependencyManifest ?? throw new ArgumentNullException(nameof(dependencyManifest));
    }
}
