// StartCommand — recrd start [--browser] [--headed] [--viewport] [--base-url] (CLI-01, D-03)

using System.CommandLine;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Recrd.Cli.Ipc;
using Recrd.Cli.Logging;
using Recrd.Cli.Output;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Core.Pipeline;
using Recrd.Recording.Engine;

namespace Recrd.Cli.Commands;

internal static class StartCommand
{
    public static Command Create(
        Option<string> verbosityOption,
        Option<string> logOutputOption,
        IRecorderEngine? engineOverride = null)
    {
        var command = new Command("start", "Start a recording session");

        var browserOption = new Option<string>("--browser")
        {
            Description = "Browser engine: chromium|firefox|webkit",
            DefaultValueFactory = _ => "chromium",
        };
        var headedOption = new Option<bool>("--headed")
        {
            Description = "Run browser in headed (visible) mode",
            DefaultValueFactory = _ => true,
        };
        var viewportOption = new Option<string>("--viewport")
        {
            Description = "Viewport size as WxH (e.g. 1280x720)",
            DefaultValueFactory = _ => "1280x720",
        };
        var baseUrlOption = new Option<string?>("--base-url")
        {
            Description = "Base URL to open when browser starts",
        };

        command.Options.Add(browserOption);
        command.Options.Add(headedOption);
        command.Options.Add(viewportOption);
        command.Options.Add(baseUrlOption);

        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var verbosity = result.GetValue(verbosityOption) ?? "normal";
            var logJson = result.GetValue(logOutputOption) == "json";
            using var loggerFactory = LoggingSetup.Create(verbosity, logJson);
            var logger = loggerFactory.CreateLogger("recrd.start");

            // D-03: Single session guard — check for existing active session
            if (await SessionSocket.IsSessionActiveAsync(SessionSocket.DefaultSocketPath))
            {
                CliOutput.WriteError("A session is already running. Use 'recrd stop' to end it first.");
                return 1;
            }

            // Parse --viewport "WxH"
            var viewportStr = result.GetValue(viewportOption) ?? "1280x720";
            ViewportSize viewport;
            try
            {
                var parts = viewportStr.Split('x', 2);
                viewport = new ViewportSize(int.Parse(parts[0]), int.Parse(parts[1]));
            }
            catch
            {
                CliOutput.WriteError($"Invalid viewport format '{viewportStr}'. Expected WxH, e.g. 1280x720.");
                return 1;
            }

            var options = new RecorderOptions
            {
                BrowserEngine = result.GetValue(browserOption) ?? "chromium",
                Headed = result.GetValue(headedOption),
                ViewportSize = viewport,
                BaseUrl = result.GetValue(baseUrlOption),
            };

            // Launch recording engine
            using var channel = new RecordingChannel();
            await using var engine = engineOverride ?? new PlaywrightRecorderEngine(channel);

            await engine.StartAsync(options, ct);

            var startTimestamp = Stopwatch.GetTimestamp();

            // Start Unix socket server and wait for stop command
            await using var sessionSocket = new SessionSocket(SessionSocket.DefaultSocketPath, logger);

            await sessionSocket.StartListeningAsync(async command =>
            {
                switch (command)
                {
                    case "pause":
                        await engine.PauseAsync(ct);
                        logger.LogInformation("Session paused");
                        break;
                    case "resume":
                        await engine.ResumeAsync(ct);
                        logger.LogInformation("Session resumed");
                        break;
                    case "stop":
                        // Stop is handled after the loop exits
                        break;
                }
            }, ct);

            // Stop command received — finalize session
            var outputPath = Path.Combine(options.OutputDirectory, "session.recrd");
            var finalSession = await engine.StopAsync(outputPath, ct);

            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var partialPath = outputPath + ".partial";
            var partialDeleted = File.Exists(partialPath);
            if (partialDeleted)
            {
                try { File.Delete(partialPath); } catch { /* best effort */ }
            }

            long outputSize = 0;
            try { outputSize = new FileInfo(outputPath).Length; } catch { /* file may not exist if engine threw */ }

            CliOutput.WriteSummary(
                events: finalSession.Steps.Count,
                variables: finalSession.Variables.Count,
                duration: elapsed,
                outputFile: outputPath,
                outputSizeBytes: outputSize,
                partialDeleted: partialDeleted);

            return 0;
        });

        return command;
    }

    // Parameterless overload for tests that only need the command structure
    internal static Command Create() => Create(
        new Option<string>("--verbosity") { DefaultValueFactory = _ => "normal" },
        new Option<string>("--log-output"));
}
