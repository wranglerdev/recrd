using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Compilers;

public sealed class RobotBrowserCompiler : ITestCompiler
{
    public string TargetName => "robot-browser";

    public Task<CompilationResult> CompileAsync(Session session, CompilerOptions options) =>
        throw new NotImplementedException();
}
