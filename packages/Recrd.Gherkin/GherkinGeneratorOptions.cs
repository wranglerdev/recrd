namespace Recrd.Gherkin;

public sealed record GherkinGeneratorOptions
{
    /// <summary>Tags to emit above the Scenario/Scenario Outline (null = no tags).</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Path to the data file being used (informational — carried in GherkinException.DataFilePath
    /// when a variable is missing from the data source).
    /// </summary>
    public string? DataFilePath { get; init; }

    /// <summary>
    /// TextWriter to receive non-fatal warnings (e.g. extra data columns).
    /// If null, warnings are silently suppressed.
    /// </summary>
    public TextWriter? WarningWriter { get; init; }
}
