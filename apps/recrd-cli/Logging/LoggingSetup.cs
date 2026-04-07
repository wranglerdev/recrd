// LoggingSetup — creates ILoggerFactory from --verbosity and --log-output flags (D-06, D-07, CLI-09, CLI-10)

using Microsoft.Extensions.Logging;

namespace Recrd.Cli.Logging;

internal static class LoggingSetup
{
    /// <summary>
    /// Creates an ILoggerFactory from parsed CLI verbosity and log-output flags.
    /// </summary>
    /// <param name="verbosity">One of: quiet, normal, detailed, diagnostic</param>
    /// <param name="jsonOutput">True to use JSON console formatter; false for plain console</param>
    public static ILoggerFactory Create(string verbosity, bool jsonOutput)
    {
        var level = verbosity switch
        {
            "quiet" => LogLevel.Error,
            "normal" => LogLevel.Information,
            "detailed" => LogLevel.Debug,
            "diagnostic" => LogLevel.Trace,
            _ => LogLevel.Information,
        };

        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(level);

            if (jsonOutput)
            {
                builder.AddJsonConsole();
            }
            else
            {
                builder.AddConsole();
            }
        });
    }
}
