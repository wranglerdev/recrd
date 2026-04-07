// SanitizeCommand — recrd sanitize <session.recrd> [--out path] (CLI-05, D-08)

using System.CommandLine;

namespace Recrd.Cli.Commands;

internal static class SanitizeCommand
{
    public static Command Create(
        Option<string>? verbosityOption = null,
        Option<string>? logOutputOption = null)
    {
        var command = new Command(
            "sanitize",
            "Strip literal values from a .recrd session; output goes to <basename>.sanitized.recrd");

        var sessionArg = new Argument<FileInfo>("session")
        {
            Description = "Path to the .recrd session file to sanitize",
        };
        var outOption = new Option<FileInfo?>("--out")
        {
            Description = "Override output path (default: <basename>.sanitized.recrd in same directory)",
        };

        command.Arguments.Add(sessionArg);
        command.Options.Add(outOption);

        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            // Stub — full sanitize implementation in Plan 03
            return await Task.FromResult(0);
        });

        return command;
    }
}
