using System.Text;
using Recrd.Compilers.Internal;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Compilers;

public sealed class RobotBrowserCompiler : ITestCompiler
{
    public string TargetName => "robot-browser";

    public async Task<CompilationResult> CompileAsync(Session session, CompilerOptions options)
    {
        Directory.CreateDirectory(options.OutputDirectory);

        var resourcePath = Path.Combine(options.OutputDirectory, "session.resource");
        var robotPath = Path.Combine(options.OutputDirectory, "session.robot");

        var warnings = new List<string>();
        var keywordNames = new HashSet<string>(StringComparer.Ordinal);

        // Collect and name all keywords, handling collisions
        var flatSteps = FlattenSteps(session.Steps);
        var keywordMap = new List<(IStep step, string name)>();

        foreach (var step in flatSteps)
        {
            string baseName = step switch
            {
                ActionStep a => KeywordNameBuilder.Build(a.ActionType,
                    a.Selector.Values.TryGetValue(options.PreferredSelectorStrategy, out var v)
                        ? v
                        : a.Selector.Strategies.Count > 0
                            ? a.Selector.Values.GetValueOrDefault(a.Selector.Strategies[0]) ?? string.Empty
                            : string.Empty),
                AssertionStep ass => KeywordNameBuilder.BuildAssertion(ass.AssertionType,
                    ass.Selector.Values.TryGetValue(options.PreferredSelectorStrategy, out var sv)
                        ? sv
                        : ass.Selector.Strategies.Count > 0
                            ? ass.Selector.Values.GetValueOrDefault(ass.Selector.Strategies[0]) ?? string.Empty
                            : string.Empty),
                _ => "Passo Desconhecido"
            };

            // Handle collisions with numeric suffix
            var name = baseName;
            int suffix = 2;
            while (!keywordNames.Add(name))
            {
                name = $"{baseName} {suffix}";
                suffix++;
            }

            keywordMap.Add((step, name));
        }

        // Write .resource file
        await using (var writer = new StreamWriter(resourcePath, append: false, new UTF8Encoding(false)))
        {
            await writer.WriteLineAsync(HeaderEmitter.Emit("robot-browser", options.SourceFilePath, session));
            await writer.WriteLineAsync("*** Settings ***");
            await writer.WriteLineAsync("Library    Browser");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("*** Keywords ***");
            await writer.WriteLineAsync("Abrir Suite");
            await writer.WriteLineAsync("    New Browser    chromium    headless=true");
            await writer.WriteLineAsync("    New Page    ${BASE_URL}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("Fechar Suite");
            await writer.WriteLineAsync("    Close Browser");

            foreach (var (step, name) in keywordMap)
            {
                await writer.WriteLineAsync();
                await writer.WriteLineAsync(name);

                switch (step)
                {
                    case ActionStep actionStep:
                        await BrowserKeywordEmitter.WriteKeywordAsync(writer, actionStep, options, warnings);
                        break;
                    case AssertionStep assertionStep:
                        await BrowserKeywordEmitter.WriteAssertionKeywordAsync(writer, assertionStep, options, warnings);
                        break;
                }
            }
        }

        // Write .robot file
        await using (var writer = new StreamWriter(robotPath, append: false, new UTF8Encoding(false)))
        {
            await writer.WriteLineAsync(HeaderEmitter.Emit("robot-browser", options.SourceFilePath, session));
            await writer.WriteLineAsync("*** Settings ***");
            await writer.WriteLineAsync("Resource    session.resource");
            await writer.WriteLineAsync("Suite Setup      Abrir Suite");
            await writer.WriteLineAsync("Suite Teardown   Fechar Suite");
            await writer.WriteLineAsync("Metadata    RF-Version    7");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("*** Variables ***");
            await writer.WriteLineAsync($"${{BASE_URL}}     {session.Metadata.BaseUrl ?? "http://localhost"}");
            await writer.WriteLineAsync($"${{TIMEOUT}}      {options.TimeoutSeconds}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("*** Test Cases ***");
            await writer.WriteLineAsync("Sessao Gravada");

            foreach (var (step, name) in keywordMap)
            {
                if (step is ActionStep a && NeedsArgument(a))
                {
                    var arg = GetArgument(a);
                    await writer.WriteLineAsync($"    {name}    {arg}");
                }
                else
                {
                    await writer.WriteLineAsync($"    {name}");
                }
            }
        }

        var dependencyManifest = new Dictionary<string, string>
        {
            ["robotframework"] = "7.x",
            ["robotframework-browser"] = "19.x"
        };

        return new CompilationResult(
            generatedFiles: [resourcePath, robotPath],
            warnings: warnings,
            dependencyManifest: dependencyManifest);
    }

    private static IEnumerable<IStep> FlattenSteps(IReadOnlyList<IStep> steps)
    {
        foreach (var step in steps)
        {
            if (step is GroupStep group)
            {
                foreach (var child in FlattenSteps(group.Steps))
                    yield return child;
            }
            else
            {
                yield return step;
            }
        }
    }

    private static bool NeedsArgument(ActionStep step) =>
        step.ActionType is ActionType.Type or ActionType.Select;

    private static string GetArgument(ActionStep step) =>
        step.Payload.GetValueOrDefault("value") ?? string.Empty;
}
