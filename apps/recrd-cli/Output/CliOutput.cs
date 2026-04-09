// CliOutput — ANSI color helpers and stop summary formatter (D-05, D-09)

namespace Recrd.Cli.Output;

internal static class CliOutput
{
    /// <summary>Writes an error message to stderr in red.</summary>
    public static void WriteError(string message)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ForegroundColor = prev;
    }

    /// <summary>Writes a success message to stdout in green.</summary>
    public static void WriteSuccess(string message)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Out.WriteLine(message);
        Console.ForegroundColor = prev;
    }

    /// <summary>Writes an informational message to stdout with default color.</summary>
    public static void WriteInfo(string message)
    {
        Console.Out.WriteLine(message);
    }

    /// <summary>
    /// Prints the stop summary block per D-09.
    /// Format:
    ///   Session complete
    ///     Events captured:  {events}
    ///     Variables:        {variables}
    ///     Duration:         {formatted}
    ///     Output:           {filename} ({sizeKB} KB)
    ///     Partial file:     {filename}.partial (deleted)   [only if partialDeleted]
    /// </summary>
    public static void WriteSummary(
        int events,
        int variables,
        TimeSpan duration,
        string outputFile,
        long outputSizeBytes,
        bool partialDeleted)
    {
        var durationFormatted = duration.TotalMinutes >= 1
            ? $"{(int)duration.TotalMinutes}m {duration.Seconds}s"
            : $"{duration.TotalSeconds:F0}s";

        var sizeKb = outputSizeBytes / 1024.0;

        Console.Out.WriteLine("Session complete");
        Console.Out.WriteLine($"  Events captured:  {events}");
        Console.Out.WriteLine($"  Variables:        {variables}");
        Console.Out.WriteLine($"  Duration:         {durationFormatted}");
        Console.Out.WriteLine($"  Output:           {outputFile} ({sizeKb:F1} KB)");

        if (partialDeleted)
        {
            Console.Out.WriteLine($"  Partial file:     {outputFile}.partial (deleted)");
        }
    }
}
