// StartCommand — recrd start [--browser] [--headed] [--viewport] [--base-url] (CLI-01, D-03)

using System.CommandLine;
using Recrd.Cli.Logging;

namespace Recrd.Cli.Commands;

internal static class StartCommand
{
    public static Command Create(
        Option<string> verbosityOption,
        Option<string> logOutputOption)
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

            // Stub — full implementation in Plan 02
            return await Task.FromResult(0);
        });

        return command;
    }

    // Parameterless overload for tests that only need the command structure
    internal static Command Create() => Create(
        new Option<string>("--verbosity") { DefaultValueFactory = _ => "normal" },
        new Option<string>("--log-output"));
}
