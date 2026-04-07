// SessionControlCommand — recrd pause / resume / stop (CLI-02, D-02, D-04)
// These are thin IPC clients — they connect to the socket, send one JSON line, and disconnect.
// No IRecorderEngine instantiation here (per Pitfall 5 from RESEARCH.md).

using System.CommandLine;
using Microsoft.Extensions.Logging;
using Recrd.Cli.Ipc;
using Recrd.Cli.Logging;
using Recrd.Cli.Output;

namespace Recrd.Cli.Commands;

internal static class SessionControlCommand
{
    public static Command CreatePause(
        Option<string>? verbosityOption = null,
        Option<string>? logOutputOption = null)
    {
        var command = new Command("pause", "Pause the active recording session");
        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var verbosity = verbosityOption is not null ? result.GetValue(verbosityOption) ?? "normal" : "normal";
            var logJson = logOutputOption is not null && result.GetValue(logOutputOption) == "json";
            using var loggerFactory = LoggingSetup.Create(verbosity, logJson);
            var logger = loggerFactory.CreateLogger("recrd.pause");

            var exitCode = await SessionClient.SendCommandAsync("pause", logger, ct);
            if (exitCode == 0)
                CliOutput.WriteSuccess("Session paused");
            return exitCode;
        });
        return command;
    }

    public static Command CreateResume(
        Option<string>? verbosityOption = null,
        Option<string>? logOutputOption = null)
    {
        var command = new Command("resume", "Resume a paused recording session");
        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var verbosity = verbosityOption is not null ? result.GetValue(verbosityOption) ?? "normal" : "normal";
            var logJson = logOutputOption is not null && result.GetValue(logOutputOption) == "json";
            using var loggerFactory = LoggingSetup.Create(verbosity, logJson);
            var logger = loggerFactory.CreateLogger("recrd.resume");

            var exitCode = await SessionClient.SendCommandAsync("resume", logger, ct);
            if (exitCode == 0)
                CliOutput.WriteSuccess("Session resumed");
            return exitCode;
        });
        return command;
    }

    public static Command CreateStop(
        Option<string>? verbosityOption = null,
        Option<string>? logOutputOption = null)
    {
        var command = new Command("stop", "Stop the active recording session and emit output");
        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var verbosity = verbosityOption is not null ? result.GetValue(verbosityOption) ?? "normal" : "normal";
            var logJson = logOutputOption is not null && result.GetValue(logOutputOption) == "json";
            using var loggerFactory = LoggingSetup.Create(verbosity, logJson);
            var logger = loggerFactory.CreateLogger("recrd.stop");

            var exitCode = await SessionClient.SendCommandAsync("stop", logger, ct);
            if (exitCode == 0)
                CliOutput.WriteSuccess("Stop signal sent");
            return exitCode;
        });
        return command;
    }
}
