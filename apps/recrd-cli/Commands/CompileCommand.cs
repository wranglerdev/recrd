// CompileCommand — recrd compile <session.recrd> [options] (CLI-03)

using System.CommandLine;
using Recrd.Cli.Logging;

namespace Recrd.Cli.Commands;

internal static class CompileCommand
{
    public static Command Create(
        Option<string>? verbosityOption = null,
        Option<string>? logOutputOption = null)
    {
        var command = new Command("compile", "Compile a .recrd session into a Robot Framework suite");

        var sessionArg = new Argument<FileInfo>("session")
        {
            Description = "Path to the .recrd session file",
        };
        var targetOption = new Option<string>("--target")
        {
            Description = "Compiler target: robot-browser|robot-selenium",
            DefaultValueFactory = _ => "robot-browser",
        };
        var dataOption = new Option<FileInfo?>("--data")
        {
            Description = "Data file (.csv or .json) for Exemplos table",
        };
        var csvDelimiterOption = new Option<char>("--csv-delimiter")
        {
            Description = "CSV field delimiter character",
            DefaultValueFactory = _ => ',',
        };
        var outOption = new Option<DirectoryInfo>("--out")
        {
            Description = "Output directory for generated files",
            DefaultValueFactory = _ => new DirectoryInfo("."),
        };
        var selectorStrategyOption = new Option<string>("--selector-strategy")
        {
            Description = "Selector priority strategy: data-testid|id|role|css|xpath",
            DefaultValueFactory = _ => "data-testid",
        };
        var timeoutOption = new Option<int>("--timeout")
        {
            Description = "Default timeout in seconds for generated keywords",
            DefaultValueFactory = _ => 30,
        };
        var interceptOption = new Option<bool>("--intercept")
        {
            Description = "Enable network intercept assertions in output",
            DefaultValueFactory = _ => false,
        };

        command.Arguments.Add(sessionArg);
        command.Options.Add(targetOption);
        command.Options.Add(dataOption);
        command.Options.Add(csvDelimiterOption);
        command.Options.Add(outOption);
        command.Options.Add(selectorStrategyOption);
        command.Options.Add(timeoutOption);
        command.Options.Add(interceptOption);

        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            // Stub — full compilation implementation in Plan 03
            return await Task.FromResult(0);
        });

        return command;
    }
}
