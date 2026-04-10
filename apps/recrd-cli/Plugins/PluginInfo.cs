namespace Recrd.Cli.Plugins;

public sealed record PluginInfo(
    string Name,
    Version? Version,
    string[] Interfaces,
    bool Loaded,
    string? Error);
