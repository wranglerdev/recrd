using Recrd.Core.Ast;

namespace Recrd.Core.Interfaces;

public interface IAssertionProvider
{
    string AssertionType { get; }
    AssertionStep CreateStep(Selector[] selectors, string? expectedValue);
}
