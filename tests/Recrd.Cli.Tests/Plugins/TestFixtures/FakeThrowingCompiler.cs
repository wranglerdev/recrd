using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Plugin.Test;

public class FakeThrowingCompiler : ITestCompiler
{
    public string TargetName => "fake-throwing";

    public Task<CompilationResult> CompileAsync(Session session, CompilerOptions options)
    {
        throw new InvalidOperationException("Plugin kaboom");
    }
}
