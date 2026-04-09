// Phase 08 — CLI tests (TDD green phase)
// This placeholder class prevents xunit no-tests-found exit code 1 on empty test projects.

using System;
using System.IO;
using Recrd.Cli.Output;
using Xunit;

namespace Recrd.Cli.Tests;

public class CliOutputTests
{
    [Fact]
    public void WriteSuccess_WritesToConsole()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(output);
        try
        {
            CliOutput.WriteSuccess("Success message");
            Assert.Contains("Success message", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteInfo_WritesToConsole()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(output);
        try
        {
            CliOutput.WriteInfo("Info message");
            Assert.Contains("Info message", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteSummary_WritesToConsole()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(output);
        try
        {
            CliOutput.WriteSummary(10, 2, TimeSpan.FromSeconds(5), "test.recrd", 1024, true);
            var result = output.ToString();
            Assert.Contains("Session complete", result);
            Assert.Contains("Events captured:  10", result);
            Assert.Contains("Variables:        2", result);
            Assert.Contains("Duration:         5s", result);
            Assert.Contains("Output:           test.recrd", result);
            Assert.Contains("1", result); 
            Assert.Contains("KB", result);
            Assert.Contains("Partial file:     test.recrd.partial (deleted)", result);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteSummary_DurationFormattedWithMinutes()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(output);
        try
        {
            CliOutput.WriteSummary(10, 2, TimeSpan.FromSeconds(65), "test.recrd", 1024, false);
            var result = output.ToString();
            Assert.Contains("Duration:         1m 5s", result);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
