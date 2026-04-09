// ValidateCommand — recrd validate <session.recrd> (CLI-04)

using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;
using Recrd.Cli.Output;
using Recrd.Core.Serialization;

namespace Recrd.Cli.Commands;

internal static class ValidateCommand
{
    private static readonly Regex VariableNamePattern = new(@"^[a-z][a-z0-9_]{0,63}$", RegexOptions.Compiled);

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
            var sessionFile = result.GetValue(sessionArg)!;

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

            // Read file content
            var json = await File.ReadAllTextAsync(sessionPath, ct);

            // Attempt deserialization
            Recrd.Core.Ast.Session? session;
            try
            {
                session = JsonSerializer.Deserialize(json, RecrdJsonContext.Default.Session);
            }
            catch (JsonException ex)
            {
                CliOutput.WriteError($"Invalid JSON: {ex.Message}");
                return 1;
            }

            if (session is null)
            {
                CliOutput.WriteError("Session file is empty or invalid.");
                return 1;
            }

            // Schema validation: SchemaVersion must be 1
            if (session.SchemaVersion != 1)
            {
                CliOutput.WriteError(
                    $"Unsupported schema version: {session.SchemaVersion}. Expected: 1");
                return 1;
            }

            // Metadata must not be null (constructor enforces this, but defensive check post-deserialization)
            if (session.Metadata is null)
            {
                CliOutput.WriteError("Session metadata is missing.");
                return 1;
            }

            // Steps must not be empty
            if (session.Steps is null || session.Steps.Count == 0)
            {
                CliOutput.WriteError("Session has no steps. At least one step is required.");
                return 1;
            }

            // Variable consistency validation
            foreach (var variable in session.Variables)
            {
                if (!VariableNamePattern.IsMatch(variable.Name))
                {
                    CliOutput.WriteError(
                        $"Invalid variable name '{variable.Name}'. Must match ^[a-z][a-z0-9_]{{0,63}}$.");
                    return 1;
                }
            }

            CliOutput.WriteSuccess("Session is valid.");
            return 0;
        });

        return command;
    }
}
