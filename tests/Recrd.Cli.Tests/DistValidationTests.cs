using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace Recrd.Cli.Tests;

public class DistValidationTests
{
    [Fact]
    public void CheckPackage_FailsWhenMissingAssets()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = RunValidation(tempDir);
            Assert.NotEqual(0, result.ExitCode);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CheckPackage_PassesWhenMockPackageProvided()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            // Mock main executable
            File.WriteAllText(Path.Combine(tempDir, "recrd"), "#!/bin/bash");
            
            // Mock .playwright
            var playwrightDir = Path.Combine(tempDir, ".playwright");
            Directory.CreateDirectory(playwrightDir);
            
            var packageDir = Path.Combine(playwrightDir, "package");
            Directory.CreateDirectory(packageDir);
            File.WriteAllText(Path.Combine(packageDir, "cli.js"), "// cli.js content");
            
            var nodeDir = Path.Combine(playwrightDir, "node", "linux-x64");
            Directory.CreateDirectory(nodeDir);
            File.WriteAllText(Path.Combine(nodeDir, "node"), "#!/bin/bash");
            
            var result = RunValidation(tempDir);
            Assert.Equal(0, result.ExitCode);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CheckPackage_FailsForActualReleasePath_TddRed()
    {
        // This test is specifically intended to fail in the Red phase
        // because the publish/ directory doesn't exist yet or is empty.
        var releasePath = Path.Combine(Directory.GetCurrentDirectory(), "publish");
        
        var result = RunValidation(releasePath);
        
        // In RED phase, we ASSERT that it passes, which will FAIL because the directory is missing.
        Assert.True(result.ExitCode == 0, $"Expected success for actual release path, but failed with: {result.Output}");
    }

    private (int ExitCode, string Output) RunValidation(string packagePath)
    {
        // Find project root
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        string? scriptPath = null;
        while (current != null)
        {
            var potential = Path.Combine(current.FullName, "scripts", "validate-pkg.sh");
            if (File.Exists(potential))
            {
                scriptPath = potential;
                break;
            }
            current = current.Parent;
        }

        if (scriptPath == null)
            throw new FileNotFoundException("Could not find scripts/validate-pkg.sh");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
             // We don't have a windows equivalent yet, so for now we skip or return failure
             return (1, "Validation script not supported on Windows yet");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"{scriptPath} {packagePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output);
    }
}
