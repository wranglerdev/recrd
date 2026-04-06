using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Compilers;

public sealed class RobotSeleniumCompiler : ITestCompiler
{
    public string TargetName => "robot-selenium";

    public Task<CompilationResult> CompileAsync(Session session, CompilerOptions options) =>
        throw new NotImplementedException();
}
