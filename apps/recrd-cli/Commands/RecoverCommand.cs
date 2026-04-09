// RecoverCommand — recrd recover [--partial-file path] (CLI-06)

using System.CommandLine;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Recrd.Cli.Logging;
using Recrd.Cli.Output;
using Recrd.Core.Pipeline;
using Recrd.Core.Serialization;
using Recrd.Recording.Engine;

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
            var verbosity = verbosityOption != null ? result.GetValue(verbosityOption) ?? "normal" : "normal";
            var logOutputFormat = logOutputOption != null ? result.GetValue(logOutputOption) ?? "" : "";
            var jsonOutput = logOutputFormat.Equals("json", StringComparison.OrdinalIgnoreCase);

            using var loggerFactory = LoggingSetup.Create(verbosity, jsonOutput);
            var logger = loggerFactory.CreateLogger("RecoverCommand");

            var partialFileInfo = result.GetValue(partialFileOption);
            string? partialPath;

            if (partialFileInfo != null)
            {
                partialPath = partialFileInfo.FullName;
            }
            else
            {
                var partials = Directory.GetFiles(".", "*.recrd.partial")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ToArray();

                if (partials.Length == 0)
                {
                    CliOutput.WriteError("No .recrd.partial files found in the current directory. Use --partial-file to specify a path.");
                    return 1;
                }

                partialPath = partials[0].FullName;
            }

            logger.LogInformation("Recovering session from {Path}", partialPath);

            try
            {
                using var channel = new RecordingChannel();
                await using var engine = new PlaywrightRecorderEngine(channel);
                var session = await engine.RecoverAsync(partialPath, ct);

                var outputPath = partialPath.EndsWith(".partial")
                    ? partialPath[..^8]
                    : partialPath + ".recrd";

                var json = JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session);
                await File.WriteAllTextAsync(outputPath, json, new UTF8Encoding(false), ct);

                CliOutput.WriteSuccess($"Session recovered: {outputPath}");
                return 0;
            }
            catch (Exception ex)
            {
                CliOutput.WriteError($"Recovery failed: {ex.Message}");
                logger.LogError(ex, "Recovery failed");
                return 1;
            }
        });

        return command;
    }
}
