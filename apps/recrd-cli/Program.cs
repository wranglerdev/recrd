// recrd CLI entry point — System.CommandLine 2.0.5 command tree (CLI-01 through CLI-12)
// Uses stable 2.0.5 API: SetAction, Options.Add, Subcommands.Add
// Does NOT use: SetHandler, AddOption, AddCommand, CommandLineBuilder (all removed in 2.0.5)

using System.CommandLine;
using Recrd.Cli.Commands;

// Shared global options — passed to subcommands that need to resolve logging
var verbosityOption = new Option<string>("--verbosity", "-v")
{
    Description = "Output verbosity: quiet|normal|detailed|diagnostic",
    DefaultValueFactory = _ => "normal",
};
var logOutputOption = new Option<string>("--log-output")
{
    Description = "Log format: json for machine-parseable output",
};

var rootCommand = new RootCommand("recrd - record browser interactions, compile Robot Framework suites");
rootCommand.Options.Add(verbosityOption);
rootCommand.Options.Add(logOutputOption);

// start
rootCommand.Subcommands.Add(StartCommand.Create(verbosityOption, logOutputOption));

// pause / resume / stop
rootCommand.Subcommands.Add(SessionControlCommand.CreatePause(verbosityOption, logOutputOption));
rootCommand.Subcommands.Add(SessionControlCommand.CreateResume(verbosityOption, logOutputOption));
rootCommand.Subcommands.Add(SessionControlCommand.CreateStop(verbosityOption, logOutputOption));

// compile
rootCommand.Subcommands.Add(CompileCommand.Create(verbosityOption, logOutputOption));

// validate
rootCommand.Subcommands.Add(ValidateCommand.Create(verbosityOption, logOutputOption));

// sanitize
rootCommand.Subcommands.Add(SanitizeCommand.Create(verbosityOption, logOutputOption));

// recover
rootCommand.Subcommands.Add(RecoverCommand.Create(verbosityOption, logOutputOption));

// version
rootCommand.Subcommands.Add(VersionCommand.Create());

// plugins (list + install subcommands grouped)
rootCommand.Subcommands.Add(PluginsCommand.Create());

return await rootCommand.Parse(args).InvokeAsync();
