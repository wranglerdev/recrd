// VersionCommand — recrd version (CLI-07)

using System.CommandLine;
using System.Reflection;

namespace Recrd.Cli.Commands;

internal static class VersionCommand
{
    public static Command Create()
    {
        var command = new Command("version", "Display recrd version and runtime information");

        command.SetAction((ParseResult result) =>
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
            var runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            Console.Out.WriteLine($"recrd {version}");
            Console.Out.WriteLine($"Runtime: {runtime}");
            return 0;
        });

        return command;
    }
}
