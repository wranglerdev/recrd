// CompileCommand — recrd compile <session.recrd> [options] (CLI-03)

using System.CommandLine;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Recrd.Cli.Logging;
using Recrd.Cli.Output;
using Recrd.Compilers;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Core.Serialization;
using Recrd.Data;
using Recrd.Gherkin;

namespace Recrd.Cli.Commands;

internal static class CompileCommand
{
    public static Command Create(
        Plugins.PluginManager? pluginManager = null,
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
            var sessionFile = result.GetValue(sessionArg)!;
            var target = result.GetValue(targetOption)!;
            var dataFile = result.GetValue(dataOption);
            var csvDelimiter = result.GetValue(csvDelimiterOption);
            var outDir = result.GetValue(outOption)!;
            var selectorStr = result.GetValue(selectorStrategyOption)!;
            var timeout = result.GetValue(timeoutOption);
            var intercept = result.GetValue(interceptOption);

            // Resolve logger
            var verbosity = verbosityOption is not null ? result.GetValue(verbosityOption) ?? "normal" : "normal";
            var logOutput = logOutputOption is not null ? result.GetValue(logOutputOption) : null;
            using var loggerFactory = LoggingSetup.Create(verbosity, logOutput == "json");
            var logger = loggerFactory.CreateLogger("compile");

            // Validate session file
            if (!sessionFile.Exists)
            {
                CliOutput.WriteError($"Session file not found: {sessionFile.FullName}");
                return 1;
            }

            var sessionPath = Path.GetFullPath(sessionFile.FullName);

            if (!sessionPath.EndsWith(".recrd", StringComparison.OrdinalIgnoreCase))
            {
                CliOutput.WriteError($"Expected a .recrd file, got: {sessionPath}");
                return 1;
            }

            // Warn if --intercept is set (reserved for Phase 11)
            if (intercept)
            {
                logger.LogWarning("--intercept flag is reserved for plugin interceptors (Phase 11)");
            }

            // Load session file
            var json = await File.ReadAllTextAsync(sessionPath, ct);
            Session? session;
            try
            {
                session = JsonSerializer.Deserialize(json, RecrdJsonContext.Default.Session);
            }
            catch (JsonException ex)
            {
                CliOutput.WriteError($"Failed to parse session file: {ex.Message}");
                return 1;
            }

            if (session is null)
            {
                CliOutput.WriteError("Failed to deserialize session file.");
                return 1;
            }

            // Resolve compiler by target name
            var compilers = new Dictionary<string, ITestCompiler>(StringComparer.OrdinalIgnoreCase)
            {
                ["robot-browser"] = new RobotBrowserCompiler(),
                ["robot-selenium"] = new RobotSeleniumCompiler()
            };

            if (pluginManager != null)
            {
                try
                {
                    foreach (var pluginCompiler in pluginManager.GetCompilers())
                    {
                        // D-07: Built-in compilers take precedence; do not override
                        if (!compilers.ContainsKey(pluginCompiler.TargetName))
                        {
                            compilers[pluginCompiler.TargetName] = pluginCompiler;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Plugin discovery failed: {ex.Message}");
                }
            }

            if (!compilers.TryGetValue(target, out var compiler))
            {
                CliOutput.WriteError(
                    $"Unknown compiler target '{target}'. Available: {string.Join(", ", compilers.Keys)}");
                return 1;
            }

            // Resolve data provider (if --data provided)
            IDataProvider? dataProvider = null;
            if (dataFile is not null)
            {
                var dataFilePath = Path.GetFullPath(dataFile.FullName);

                if (!dataFile.Exists)
                {
                    CliOutput.WriteError($"Data file not found: {dataFilePath}");
                    return 1;
                }

                if (dataFilePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    dataProvider = new CsvDataProvider(dataFilePath, csvDelimiter.ToString());
                }
                else if (dataFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    dataProvider = new JsonDataProvider(dataFilePath);
                }
                else
                {
                    CliOutput.WriteError("Unsupported data file format. Use .csv or .json.");
                    return 1;
                }
            }

            // Parse selector strategy
            var strategy = selectorStr.ToLowerInvariant() switch
            {
                "data-testid" => SelectorStrategy.DataTestId,
                "id" => SelectorStrategy.Id,
                "role" => SelectorStrategy.Role,
                "css" => SelectorStrategy.Css,
                "xpath" => SelectorStrategy.XPath,
                _ => SelectorStrategy.DataTestId
            };

            // Build CompilerOptions
            var outputDir = Path.GetFullPath(outDir.FullName);
            Directory.CreateDirectory(outputDir);

            var options = new CompilerOptions
            {
                OutputDirectory = outputDir,
                PreferredSelectorStrategy = strategy,
                TimeoutSeconds = timeout,
                SourceFilePath = sessionPath
            };

            // Compile
            var compilationResult = (pluginManager != null && pluginManager.IsPluginCompiler(compiler))
                ? await pluginManager.SafeCompileAsync(compiler, session, options)
                : await compiler.CompileAsync(session, options);

            // Print warnings if any
            foreach (var warning in compilationResult.Warnings)
            {
                CliOutput.WriteWarning(warning);
            }

            // Generate .feature file via GherkinGenerator
            var featurePath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(sessionFile.Name) + ".feature");
            await using var featureWriter = new StreamWriter(featurePath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            var gherkin = new GherkinGenerator();
            await gherkin.GenerateAsync(session, dataProvider, featureWriter, ct: ct);

            // Print results
            foreach (var generatedFile in compilationResult.GeneratedFiles)
            {
                CliOutput.WriteInfo(generatedFile);
            }
            CliOutput.WriteInfo(featurePath);

            // Print warnings to stderr
            foreach (var warning in compilationResult.Warnings)
            {
                CliOutput.WriteError($"Warning: {warning}");
            }

            CliOutput.WriteSuccess($"Compilation complete: {compilationResult.GeneratedFiles.Count + 1} files generated");
            return 0;
        });

        return command;
    }
}
