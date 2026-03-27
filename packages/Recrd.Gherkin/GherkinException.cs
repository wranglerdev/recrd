namespace Recrd.Gherkin;

public sealed class GherkinException : Exception
{
    public string VariableName { get; }
    public string DataFilePath { get; }

    public GherkinException(
        string variableName,
        string dataFilePath,
        string message)
        : base(message)
    {
        VariableName = variableName;
        DataFilePath = dataFilePath;
    }
}
