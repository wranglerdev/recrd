using Recrd.Compilers;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Compilers.Tests;

public class BrowserCompilerOutputTests
{
    private static Session MakeSession(params IStep[] steps) =>
        new(SchemaVersion: 1,
            Metadata: new SessionMetadata("test-id", DateTimeOffset.UtcNow, "chromium",
                new ViewportSize(1280, 720), "http://localhost"),
            Variables: [],
            Steps: steps);

    private static Selector MakeSelector(SelectorStrategy strategy, string value) =>
        new([strategy], new Dictionary<SelectorStrategy, string> { [strategy] = value });

    private static ActionStep MakeClickStep(SelectorStrategy strategy = SelectorStrategy.DataTestId, string value = "submit-btn") =>
        new(ActionType.Click, MakeSelector(strategy, value), new Dictionary<string, string>());

    [Fact]
    public async Task CompileAsync_EmitsResourceFile()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            Assert.Contains(result.GeneratedFiles, f => f.EndsWith(".resource"));
            Assert.Contains(result.GeneratedFiles, f => File.Exists(f) && f.EndsWith(".resource"));
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task CompileAsync_EmitsRobotFile()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            Assert.Contains(result.GeneratedFiles, f => f.EndsWith(".robot"));
            Assert.Contains(result.GeneratedFiles, f => File.Exists(f) && f.EndsWith(".robot"));
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task ResourceFile_ContainsSettingsWithBrowserLibrary()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains("*** Settings ***", content);
            Assert.Contains("Library    Browser", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task RobotFile_ContainsSettingsWithResourceImport()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var robotPath = result.GeneratedFiles.Single(f => f.EndsWith(".robot"));
            var content = await File.ReadAllTextAsync(robotPath);
            Assert.Contains("Resource    session.resource", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task RobotFile_ContainsMetadataRFVersion()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var robotPath = result.GeneratedFiles.Single(f => f.EndsWith(".robot"));
            var content = await File.ReadAllTextAsync(robotPath);
            Assert.Contains("Metadata    RF-Version    7", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task RobotFile_ContainsTestCaseCallingKeywords()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var robotPath = result.GeneratedFiles.Single(f => f.EndsWith(".robot"));
            var content = await File.ReadAllTextAsync(robotPath);
            Assert.Contains("*** Test Cases ***", content);
            Assert.Contains("Clicar Em Submit Btn", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task ResourceFile_ContainsKeywordDefinition()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains("*** Keywords ***", content);
            Assert.Contains("Clicar Em Submit Btn", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }
}
