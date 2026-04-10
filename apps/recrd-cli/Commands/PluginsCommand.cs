// PluginsCommand — recrd plugins list / plugins install <package> (CLI-08)

using System.CommandLine;
using Recrd.Cli.Output;

namespace Recrd.Cli.Commands;

internal static class PluginsCommand
{
    public static Command Create(Plugins.PluginManager pluginManager)
    {
        var command = new Command("plugins", "Manage recrd plugins");

        // plugins list subcommand
        var listCommand = new Command("list", "List installed plugins from ~/.recrd/plugins/");
        listCommand.SetAction((ParseResult result) =>
        {
            var plugins = pluginManager.DiscoverPlugins();

            if (plugins.Count == 0)
            {
                Console.Out.WriteLine("No plugins installed");
                return 0;
            }

            Console.Out.WriteLine("Installed plugins:");
            Console.Out.WriteLine($"  {"Name",-30} {"Version",-8} {"Interfaces",-25} {"Status"}");
            Console.Out.WriteLine($"  {new string('-', 30)} {new string('-', 8)} {new string('-', 25)} {new string('-', 10)}");

            foreach (var plugin in plugins)
            {
                var version = plugin.Version?.ToString() ?? "unknown";
                var interfaces = string.Join(",", plugin.Interfaces);
                var status = plugin.Loaded ? "✓ loaded" : $"✗ {plugin.Error ?? "load error"}";

                Console.Out.WriteLine($"  {plugin.Name,-30} {version,-8} {interfaces,-25} {status}");
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
            
            Console.Out.WriteLine($"To install {package}:");
            Console.Out.WriteLine($"  1. dotnet publish {package} -c Release --no-self-contained");
            Console.Out.WriteLine($"  2. Copy the publish output to ~/.recrd/plugins/{package}/");
            Console.Out.WriteLine();
            Console.Out.WriteLine($"The directory must contain {package}.dll and {package}.deps.json.");
            
            return 0;
        });

        command.Subcommands.Add(listCommand);
        command.Subcommands.Add(installCommand);

        return command;
    }
}
