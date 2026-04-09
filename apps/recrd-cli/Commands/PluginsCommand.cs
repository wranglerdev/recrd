// PluginsCommand — recrd plugins list / plugins install <package> (CLI-08)

using System.CommandLine;
using Recrd.Cli.Output;

namespace Recrd.Cli.Commands;

internal static class PluginsCommand
{
    public static Command Create(string? pluginsDirOverride = null)
    {
        var command = new Command("plugins", "Manage recrd plugins");

        // plugins list subcommand
        var listCommand = new Command("list", "List installed plugins from ~/.recrd/plugins/");
        listCommand.SetAction((ParseResult result) =>
        {
            var pluginsDir = pluginsDirOverride ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".recrd", "plugins");

            if (!Directory.Exists(pluginsDir))
            {
                Console.Out.WriteLine("No plugins installed");
                return 0;
            }

            var dlls = Directory.GetFiles(pluginsDir, "*.dll");

            if (dlls.Length == 0)
            {
                Console.Out.WriteLine("No plugins installed");
                return 0;
            }

            Console.Out.WriteLine("Installed plugins:");
            foreach (var dll in dlls)
            {
                Console.Out.WriteLine($"  - {Path.GetFileNameWithoutExtension(dll)}");
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
        installCommand.SetAction((ParseResult result) =>
        {
            var package = result.GetValue(packageArg);
            var pluginsDir = pluginsDirOverride ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".recrd", "plugins");

            CliOutput.WriteInfo($"Plugin installation for '{package}' is not yet implemented.");
            CliOutput.WriteInfo($"To install a plugin manually, place the plugin DLL in {pluginsDir}");
            return 1;
        });

        command.Subcommands.Add(listCommand);
        command.Subcommands.Add(installCommand);

        return command;
    }
}
