// PluginsCommand — recrd plugins list / plugins install <package> (CLI-08)

using System.CommandLine;

namespace Recrd.Cli.Commands;

internal static class PluginsCommand
{
    public static Command Create()
    {
        var command = new Command("plugins", "Manage recrd plugins");

        // plugins list subcommand
        var listCommand = new Command("list", "List installed plugins from ~/.recrd/plugins/");
        listCommand.SetAction((ParseResult result) =>
        {
            var pluginsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".recrd", "plugins");

            if (!Directory.Exists(pluginsDir) || !Directory.EnumerateFileSystemEntries(pluginsDir).Any())
            {
                Console.Out.WriteLine("No plugins installed");
                return 0;
            }

            foreach (var entry in Directory.EnumerateFileSystemEntries(pluginsDir))
            {
                Console.Out.WriteLine(Path.GetFileName(entry));
            }

            return 0;
        });

        // plugins install subcommand
        var packageArg = new Argument<string>("package")
        {
            Description = "NuGet package name (e.g. Recrd.Plugin.MyPlugin)",
        };
        var installCommand = new Command("install", "Install a plugin from NuGet");
        installCommand.Arguments.Add(packageArg);
        installCommand.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            // Stub — full plugin install implementation in Plan 04
            return await Task.FromResult(0);
        });

        command.Subcommands.Add(listCommand);
        command.Subcommands.Add(installCommand);

        return command;
    }
}
