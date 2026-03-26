using Recrd.Core.Ast;

namespace Recrd.Core.Interfaces;

public interface IAssertionProvider
{
    string AssertionName { get; }
    AssertionStep CreateAssertion(Selector selector, IReadOnlyDictionary<string, string> payload);
}
