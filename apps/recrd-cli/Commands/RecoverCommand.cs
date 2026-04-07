// RecoverCommand — recrd recover [--partial-file path] (CLI-06)

using System.CommandLine;

namespace Recrd.Cli.Commands;

internal static class RecoverCommand
{
    public static Command Create(
        Option<string>? verbosityOption = null,
        Option<string>? logOutputOption = null)
    {
        var command = new Command(
            "recover",
            "Reconstruct a session from the newest .recrd.partial snapshot");

        var partialFileOption = new Option<FileInfo?>("--partial-file")
        {
            Description = "Explicit partial file path (default: newest .recrd.partial in current directory)",
        };
        command.Options.Add(partialFileOption);

        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            // Stub — full recovery implementation in Plan 02
            return await Task.FromResult(0);
        });

        return command;
    }
}
