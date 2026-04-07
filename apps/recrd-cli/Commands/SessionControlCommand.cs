// SessionControlCommand — recrd pause / resume / stop (CLI-02, D-02, D-04)

using System.CommandLine;
using Recrd.Cli.Logging;

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
            // Stub — sends {"command":"pause"} over session.sock (Plan 02)
            return await Task.FromResult(0);
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
            // Stub — sends {"command":"resume"} over session.sock (Plan 02)
            return await Task.FromResult(0);
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
            // Stub — sends {"command":"stop"} over session.sock, prints summary (Plan 02)
            return await Task.FromResult(0);
        });
        return command;
    }
}
