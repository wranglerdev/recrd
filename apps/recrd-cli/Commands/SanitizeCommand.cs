// SanitizeCommand — recrd sanitize <session.recrd> [--out path] (CLI-05, D-08)

using System.CommandLine;
using System.Text;
using System.Text.Json;
using Recrd.Cli.Output;
using Recrd.Core.Ast;
using Recrd.Core.Serialization;

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
            var sessionFile = result.GetValue(sessionArg)!;
            var outFile = result.GetValue(outOption);

            // Validate file exists
            if (!sessionFile.Exists)
            {
                CliOutput.WriteError($"Session file not found: {sessionFile.FullName}");
                return 1;
            }

            var sessionPath = Path.GetFullPath(sessionFile.FullName);

            // Validate extension
            if (!sessionPath.EndsWith(".recrd", StringComparison.OrdinalIgnoreCase))
            {
                CliOutput.WriteError($"Expected a .recrd file, got: {sessionPath}");
                return 1;
            }

            // Read and deserialize session
            var json = await File.ReadAllTextAsync(sessionPath, ct);
            Session? session;
            try
            {
                session = JsonSerializer.Deserialize(json, RecrdJsonContext.Default.Session);
            }
            catch (JsonException ex)
            {
                CliOutput.WriteError($"Failed to parse session file: {ex.Message}");
                return 1;
            }

            if (session is null)
            {
                CliOutput.WriteError("Failed to deserialize session file.");
                return 1;
            }

            // Strip literal values — deep copy with sanitized fields
            var sanitizedSteps = SanitizeSteps(session.Steps);

            // Construct sanitized Session (keep Variables and Metadata unchanged)
            var sanitized = new Session(session.SchemaVersion, session.Metadata, session.Variables, sanitizedSteps);

            // Determine output path
            string outputPath;
            if (outFile is not null)
            {
                outputPath = Path.GetFullPath(outFile.FullName);
            }
            else
            {
                outputPath = Path.Combine(
                    Path.GetDirectoryName(sessionPath)!,
                    Path.GetFileNameWithoutExtension(sessionPath) + ".sanitized.recrd");
            }

            // Serialize and write (no BOM per T-8-08)
            var outputJson = JsonSerializer.Serialize(sanitized, RecrdJsonContext.Default.Session);
            await File.WriteAllTextAsync(outputPath, outputJson, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ct);

            CliOutput.WriteSuccess($"Sanitized session written to {outputPath}");
            return 0;
        });

        return command;
    }

    private static IReadOnlyList<IStep> SanitizeSteps(IReadOnlyList<IStep> steps)
    {
        var result = new List<IStep>(steps.Count);
        foreach (var step in steps)
        {
            result.Add(SanitizeStep(step));
        }
        return result;
    }

    private static IStep SanitizeStep(IStep step) => step switch
    {
        ActionStep actionStep => new ActionStep(
            actionStep.ActionType,
            SanitizeSelector(actionStep.Selector),
            new Dictionary<string, string>()),

        AssertionStep assertionStep => new AssertionStep(
            assertionStep.AssertionType,
            SanitizeSelector(assertionStep.Selector),
            new Dictionary<string, string>()),

        GroupStep groupStep => new GroupStep(
            groupStep.GroupType,
            SanitizeSteps(groupStep.Steps)),

        _ => step
    };

    private static Selector SanitizeSelector(Selector selector)
    {
        var sanitizedValues = new Dictionary<SelectorStrategy, string>(
            selector.Values.Select(kv => KeyValuePair.Create(kv.Key, "***")));
        return new Selector(selector.Strategies, sanitizedValues);
    }
}
