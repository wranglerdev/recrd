namespace Recrd.Core.Exceptions;

public sealed class DataParseException : Exception
{
    public int? LineNumber { get; }

    public DataParseException(string message, int? lineNumber = null)
        : base(message)
    {
        LineNumber = lineNumber;
    }

    public DataParseException(string message, int? lineNumber, Exception inner)
        : base(message, inner)
    {
        LineNumber = lineNumber;
    }
}
