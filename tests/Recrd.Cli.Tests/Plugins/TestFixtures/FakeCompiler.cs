using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Plugin.Test;

public class FakeCompiler : ITestCompiler
{
    public string TargetName => "fake-compiler";

    public Task<CompilationResult> CompileAsync(Session session, CompilerOptions options)
    {
        var result = new CompilationResult(
            generatedFiles: new[] { "output/fake-output.robot" },
            warnings: Array.Empty<string>(),
            dependencyManifest: new Dictionary<string, string>());

        return Task.FromResult(result);
    }
}
