// ValidateCommand — recrd validate <session.recrd> (CLI-04)

using System.CommandLine;

namespace Recrd.Cli.Commands;

internal static class ValidateCommand
{
    public static Command Create(
        Option<string>? verbosityOption = null,
        Option<string>? logOutputOption = null)
    {
        var command = new Command("validate", "Validate a .recrd session file against the AST schema");

        var sessionArg = new Argument<FileInfo>("session")
        {
            Description = "Path to the .recrd session file to validate",
        };
        command.Arguments.Add(sessionArg);

        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            // Stub — full validation implementation in Plan 03
            return await Task.FromResult(0);
        });

        return command;
    }
}
