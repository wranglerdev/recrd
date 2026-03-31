using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Core.Tests;

public sealed class InterfaceContractTests
{
    [Fact]
    public void ITestCompiler_HasTargetNameAndCompileAsync()
    {
        var type = typeof(ITestCompiler);

        var targetNameProp = type.GetProperty("TargetName");
        Assert.NotNull(targetNameProp);
        Assert.Equal(typeof(string), targetNameProp!.PropertyType);

        var compileMethod = type.GetMethod("CompileAsync");
        Assert.NotNull(compileMethod);
        Assert.Equal(typeof(Task<CompilationResult>), compileMethod!.ReturnType);

        var parameters = compileMethod.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Session), parameters[0].ParameterType);
        Assert.Equal(typeof(CompilerOptions), parameters[1].ParameterType);
    }

    [Fact]
    public void IDataProvider_HasStreamAsyncReturningAsyncEnumerable()
    {
        var type = typeof(IDataProvider);

        var streamMethod = type.GetMethod("StreamAsync");
        Assert.NotNull(streamMethod);

        var returnType = streamMethod!.ReturnType;
        Assert.True(
            returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>),
            $"Expected IAsyncEnumerable<T> but got {returnType.Name}"
        );

        var elementType = returnType.GetGenericArguments()[0];
        Assert.True(
            elementType.IsGenericType &&
            elementType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>),
            $"Expected IReadOnlyDictionary<string, string> element but got {elementType.Name}"
        );

        var keyValueTypes = elementType.GetGenericArguments();
        Assert.Equal(typeof(string), keyValueTypes[0]);
        Assert.Equal(typeof(string), keyValueTypes[1]);
    }

    [Fact]
    public void IEventInterceptor_InterfaceExists()
    {
        var type = typeof(IEventInterceptor);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void IAssertionProvider_InterfaceExists()
    {
        var type = typeof(IAssertionProvider);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void RecrdCore_HasZeroRecrdPackageDependencies()
    {
        var repoRoot = FindRepoRoot();
        var csprojPath = Path.Combine(repoRoot, "packages", "Recrd.Core", "Recrd.Core.csproj");

        Assert.True(File.Exists(csprojPath), $"Recrd.Core.csproj not found at {csprojPath}");

        var doc = XDocument.Load(csprojPath);
        XNamespace ns = "";

        var projectRefs = doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? "")
            .Where(v => v.Contains("Recrd.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var packageRefs = doc.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value ?? "")
            .Where(v => v.Contains("Recrd.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(projectRefs);
        Assert.Empty(packageRefs);
    }

    [Fact]
    public void CompilationResult_StoresPropertiesAndRejectsNulls()
    {
        var files = new List<string> { "out.robot" };
        var warnings = new List<string> { "warn" };
        var deps = new Dictionary<string, string> { ["lib"] = "1.0" };

        var result = new CompilationResult(files, warnings, deps);

        Assert.Same(files, result.GeneratedFiles);
        Assert.Same(warnings, result.Warnings);
        Assert.Same(deps, result.DependencyManifest);

        Assert.Throws<ArgumentNullException>(() => new CompilationResult(null!, warnings, deps));
        Assert.Throws<ArgumentNullException>(() => new CompilationResult(files, null!, deps));
        Assert.Throws<ArgumentNullException>(() => new CompilationResult(files, warnings, null!));
    }

    [Fact]
    public void CompilerOptions_HasExpectedDefaults()
    {
        var opts = new CompilerOptions();

        Assert.Equal(".", opts.OutputDirectory);
        Assert.Equal(SelectorStrategy.DataTestId, opts.PreferredSelectorStrategy);
        Assert.Equal(30, opts.TimeoutSeconds);
    }

    [Fact]
    public void RecorderOptions_HasExpectedDefaults()
    {
        var opts = new RecorderOptions();

        Assert.Equal("chromium", opts.BrowserEngine);
        Assert.True(opts.Headed);
        Assert.Equal(1280, opts.ViewportSize.Width);
        Assert.Equal(720, opts.ViewportSize.Height);
        Assert.Null(opts.BaseUrl);
        Assert.Equal(".", opts.OutputDirectory);
        Assert.Equal(TimeSpan.FromSeconds(30), opts.SnapshotInterval);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "recrd.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not find repo root containing recrd.sln");
    }
}
