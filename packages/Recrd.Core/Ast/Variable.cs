using System.Text.RegularExpressions;

namespace Recrd.Core.Ast;

public sealed partial record Variable
{
    [GeneratedRegex(@"^[a-z][a-z0-9_]{0,63}$")]
    private static partial Regex NamePattern();

    public string Name { get; }
    public string? StepRef { get; init; }

    public Variable(string name, string? stepRef = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (!NamePattern().IsMatch(name))
            throw new ArgumentException(
                $"Variable name '{name}' does not match pattern ^[a-z][a-z0-9_]{{0,63}}$.",
                nameof(name));
        Name = name;
        StepRef = stepRef;
    }
}
