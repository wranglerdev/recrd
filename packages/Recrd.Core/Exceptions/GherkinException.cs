namespace Recrd.Core.Exceptions;

public sealed class GherkinException : Exception
{
    public GherkinException(string message) : base(message) { }
    public GherkinException(string message, Exception inner) : base(message, inner) { }
}
