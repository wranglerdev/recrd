namespace Recrd.Gherkin;

public sealed record GherkinGeneratorOptions
{
    public IReadOnlyList<string>? Tags { get; init; }
}
