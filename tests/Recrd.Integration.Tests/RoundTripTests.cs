using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Recrd.Compilers;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Integration.Tests;

[Trait("Category", "Integration")]
public class RoundTripTests : IAsyncLifetime
{
    private IHost? _host;
    private string _baseUrl = string.Empty;

    private const string FixtureHtml = @"<!DOCTYPE html>
<html>
<head><title>recrd fixture</title></head>
<body>
  <a id=""nav-link"" href=""/page2"" data-testid=""nav-link"">Go to Page 2</a>
  <button id=""submit-btn"" data-testid=""submit-btn"">Submit</button>
  <input id=""email-input"" data-testid=""email-input"" type=""text"" />
  <select id=""country-select"" data-testid=""country-select"">
    <option value=""br"">Brazil</option>
    <option value=""us"">USA</option>
  </select>
  <input id=""file-upload"" data-testid=""file-upload"" type=""file"" />
  <div id=""drag-source"" data-testid=""drag-source"" draggable=""true"">Drag me</div>
  <div id=""drop-target"" data-testid=""drop-target"">Drop here</div>
  <p id=""result-text"" data-testid=""result-text"">Hello World</p>
</body>
</html>";

    public async Task InitializeAsync()
    {
        var port = GetFreePort();
        _baseUrl = $"http://localhost:{port}";

        _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(wb =>
            {
                wb.UseUrls(_baseUrl);
                wb.Configure(app =>
                {
                    app.Run(async ctx =>
                    {
                        ctx.Response.ContentType = "text/html";
                        await ctx.Response.WriteAsync(FixtureHtml);
                    });
                });
            })
            .Build();

        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
            await _host.StopAsync();
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static Session BuildFixtureSession(string baseUrl) =>
        new(SchemaVersion: 1,
            Metadata: new SessionMetadata("e2e-test", DateTimeOffset.UtcNow, "chromium",
                new ViewportSize(1280, 720), baseUrl),
            Variables: [],
            Steps: [
                new ActionStep(ActionType.Navigate,
                    new Selector([], new Dictionary<SelectorStrategy, string>()),
                    new Dictionary<string, string> { ["url"] = baseUrl }),
                new ActionStep(ActionType.Click,
                    new Selector([SelectorStrategy.DataTestId],
                        new Dictionary<SelectorStrategy, string> { [SelectorStrategy.DataTestId] = "submit-btn" }),
                    new Dictionary<string, string>()),
                new ActionStep(ActionType.Type,
                    new Selector([SelectorStrategy.DataTestId],
                        new Dictionary<SelectorStrategy, string> { [SelectorStrategy.DataTestId] = "email-input" }),
                    new Dictionary<string, string> { ["value"] = "test@test.com" }),
                new ActionStep(ActionType.Select,
                    new Selector([SelectorStrategy.DataTestId],
                        new Dictionary<SelectorStrategy, string> { [SelectorStrategy.DataTestId] = "country-select" }),
                    new Dictionary<string, string> { ["value"] = "br" }),
            ]);

    private static async Task<(int exitCode, string stdout, string stderr)> RunRobotAsync(string outDir)
    {
        var robotFile = Path.Combine(outDir, "session.robot");
        var psi = new ProcessStartInfo("python3", $"-m robot --outputdir \"{outDir}\" \"{robotFile}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stdout, stderr);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BrowserCompiler_RoundTrip_RobotExitCodeZero()
    {
        var session = BuildFixtureSession(_baseUrl);
        var outDir = Path.Combine(Path.GetTempPath(), $"recrd-e2e-browser-{Guid.NewGuid()}");
        try
        {
            var compiler = new RobotBrowserCompiler();
            var result = await compiler.CompileAsync(session, new CompilerOptions
            {
                OutputDirectory = outDir,
                TimeoutSeconds = 30,
            });
            Assert.Equal(2, result.GeneratedFiles.Count);
            Assert.All(result.GeneratedFiles, f => Assert.True(File.Exists(f)));

            var (exitCode, stdout, stderr) = await RunRobotAsync(outDir);
            Assert.True(exitCode == 0,
                $"robot exited {exitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SeleniumCompiler_RoundTrip_RobotExitCodeZero()
    {
        var session = BuildFixtureSession(_baseUrl);
        var outDir = Path.Combine(Path.GetTempPath(), $"recrd-e2e-selenium-{Guid.NewGuid()}");
        try
        {
            var compiler = new RobotSeleniumCompiler();
            var result = await compiler.CompileAsync(session, new CompilerOptions
            {
                OutputDirectory = outDir,
                TimeoutSeconds = 30,
            });
            Assert.Equal(2, result.GeneratedFiles.Count);
            Assert.All(result.GeneratedFiles, f => Assert.True(File.Exists(f)));

            var (exitCode, stdout, stderr) = await RunRobotAsync(outDir);
            Assert.True(exitCode == 0,
                $"robot exited {exitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompilationResult_HasNonEmptyGeneratedFiles()
    {
        var session = BuildFixtureSession(_baseUrl);

        var browserOutDir = Path.Combine(Path.GetTempPath(), $"recrd-e2e-browser-check-{Guid.NewGuid()}");
        var seleniumOutDir = Path.Combine(Path.GetTempPath(), $"recrd-e2e-selenium-check-{Guid.NewGuid()}");
        try
        {
            var browserResult = await new RobotBrowserCompiler().CompileAsync(session, new CompilerOptions
            {
                OutputDirectory = browserOutDir,
            });
            Assert.True(browserResult.GeneratedFiles.Count >= 2,
                $"BrowserCompiler returned only {browserResult.GeneratedFiles.Count} files");

            var seleniumResult = await new RobotSeleniumCompiler().CompileAsync(session, new CompilerOptions
            {
                OutputDirectory = seleniumOutDir,
            });
            Assert.True(seleniumResult.GeneratedFiles.Count >= 2,
                $"SeleniumCompiler returned only {seleniumResult.GeneratedFiles.Count} files");
        }
        finally
        {
            if (Directory.Exists(browserOutDir)) Directory.Delete(browserOutDir, recursive: true);
            if (Directory.Exists(seleniumOutDir)) Directory.Delete(seleniumOutDir, recursive: true);
        }
    }
}
