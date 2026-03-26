using Recrd.Core.Ast;

namespace Recrd.Core.Interfaces;

public interface ITestCompiler
{
    string TargetName { get; }
    Task<CompilationResult> CompileAsync(Session session, CompilerOptions options);
}
