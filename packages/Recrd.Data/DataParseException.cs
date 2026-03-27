namespace Recrd.Data;

public sealed class DataParseException : Exception
{
    public int LineNumber { get; }
    public string OffendingLine { get; }
    public string FilePath { get; }

    public DataParseException(
        int lineNumber,
        string offendingLine,
        string filePath,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        LineNumber = lineNumber;
        OffendingLine = offendingLine;
        FilePath = filePath;
    }
}
