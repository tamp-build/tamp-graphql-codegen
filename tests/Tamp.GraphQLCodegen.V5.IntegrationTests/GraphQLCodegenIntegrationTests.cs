using System.IO;
using Tamp;
using Xunit;
using Xunit.Abstractions;

namespace Tamp.GraphQLCodegen.V5.IntegrationTests;

/// <summary>
/// Exercises the wrapper against a real graphql-code-generator 5.x
/// install. Stages a tiny fixture per test (a schema + an operation +
/// a codegen config) so the verbs have something to chew on.
/// </summary>
public sealed class GraphQLCodegenIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly AbsolutePath _workdir;

    public GraphQLCodegenIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _workdir = AbsolutePath.Create(Path.Combine(Path.GetTempPath(), $"tamp-codegen-it-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(_workdir.Value);

        // Minimal schema + operation. typescript-operations is one of the
        // bundled plugins that ships with @graphql-codegen/cli@5, so the
        // fixture doesn't pull anything else from npm at test time.
        File.WriteAllText(Path.Combine(_workdir.Value, "schema.graphql"), """
            type Query {
              hello(name: String!): String!
            }
            """);

        File.WriteAllText(Path.Combine(_workdir.Value, "operations.graphql"), """
            query Hello($name: String!) {
              hello(name: $name)
            }
            """);

        // codegen.yml: one output file, no plugin install needed.
        // typescript + typescript-operations are bundled with the CLI v5.
        File.WriteAllText(Path.Combine(_workdir.Value, "codegen.yml"), """
            schema: schema.graphql
            documents: operations.graphql
            generates:
              types.ts:
                plugins:
                  - typescript
                  - typescript-operations
            """);
    }

    /// <summary>
    /// Walks PATH for graphql-codegen. Handles Windows extensions
    /// (cmd/exe/bat/ps1). Returns null if nothing found — caller
    /// decides whether that's fatal for the specific test.
    /// </summary>
    private static string? ResolveOnPath(string baseName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var names = OperatingSystem.IsWindows()
            ? new[] { $"{baseName}.cmd", $"{baseName}.exe", $"{baseName}.bat", $"{baseName}.ps1", baseName }
            : new[] { baseName };
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrEmpty(dir)) continue;
            foreach (var n in names)
            {
                var c = Path.Combine(dir, n);
                if (File.Exists(c)) return c;
            }
        }
        return null;
    }

    private static Tool ResolveTool()
    {
        var p = ResolveOnPath("graphql-codegen")
            ?? throw new InvalidOperationException("graphql-codegen not found on PATH. Install: npm i -g @graphql-codegen/cli@5 graphql");
        return new Tool(AbsolutePath.Create(p));
    }

    /// <summary>
    /// graphql-codegen resolves plugin packages (typescript,
    /// typescript-operations, …) from the CWD's node_modules. Since
    /// our fixture is a bare temp dir, we set NODE_PATH to the global
    /// node_modules path so node falls back to the globally-installed
    /// plugins. <c>npm root -g</c> returns the path; if npm isn't on
    /// PATH the test skips this resolution and lets the call fail
    /// loudly.
    /// </summary>
    private static string? GetGlobalNodeModules()
    {
        var npmExe = ResolveOnPath("npm");
        if (npmExe is null) return null;
        var psi = new System.Diagnostics.ProcessStartInfo(npmExe)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("root");
        psi.ArgumentList.Add("-g");
        using var p = System.Diagnostics.Process.Start(psi);
        if (p is null) return null;
        var stdout = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        return string.IsNullOrEmpty(stdout) ? null : stdout;
    }

    private CommandPlan WithNodePath(CommandPlan plan)
    {
        var globalRoot = GetGlobalNodeModules();
        if (globalRoot is null) return plan;
        var env = new Dictionary<string, string>(plan.Environment) { ["NODE_PATH"] = globalRoot };
        return plan with { Environment = env };
    }

    private CaptureResult Run(CommandPlan plan)
    {
        _output.WriteLine($"$ {plan.Executable} {string.Join(' ', plan.Arguments)}");
        var result = ProcessRunner.Capture(plan);
        foreach (var line in result.Lines)
            _output.WriteLine($"  [{line.Type}] {line.Text}");
        _output.WriteLine($"  → exit {result.ExitCode}");
        return result;
    }

    [Fact]
    public void Generate_With_Default_Config_Produces_types_ts()
    {
        var tool = ResolveTool();
        var plan = WithNodePath(GraphQLCodegen.Generate(tool, s => s
            .SetConfig("codegen.yml")
            .SetSilent()
            .SetWorkingDirectory(_workdir.Value)));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var typesPath = Path.Combine(_workdir.Value, "types.ts");
        Assert.True(File.Exists(typesPath), $"Expected {typesPath} to exist after codegen.");
        var content = File.ReadAllText(typesPath);
        Assert.Contains("HelloQuery", content);
    }

    [Fact]
    public void Generate_With_Verbose_Prints_Plan_To_Stderr_Or_Stdout()
    {
        var tool = ResolveTool();
        var plan = WithNodePath(GraphQLCodegen.Generate(tool, s => s
            .SetConfig("codegen.yml")
            .SetVerbose()
            .SetWorkingDirectory(_workdir.Value)));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        // codegen v5 prints its task summary; we don't assert on exact
        // wording (it shifts between minor releases), just that SOMETHING
        // came out across both streams when verbose is on.
        Assert.True(result.StdoutText.Length + result.StderrText.Length > 0);
    }

    [Fact]
    public void Generate_With_Missing_Config_Exits_Non_Zero()
    {
        var tool = ResolveTool();
        var plan = GraphQLCodegen.Generate(tool, s => s
            .SetConfig("does-not-exist.yml")
            .SetWorkingDirectory(_workdir.Value));
        var result = Run(plan);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public void Raw_Help_Returns_Usage_Text()
    {
        // Note on --version: globally-installed codegen frequently reports
        // "unknown" because it can't resolve its own package.json relative
        // to CWD. So this test uses --help instead — it's the more
        // load-bearing "does the binary respond" smoke check.
        var tool = ResolveTool();
        var plan = GraphQLCodegen.Raw(tool, "--help");
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var combined = result.StdoutText + result.StderrText;
        Assert.Contains("Options:", combined);
        Assert.Contains("--config", combined);
    }

    public void Dispose()
    {
        try { Directory.Delete(_workdir.Value, recursive: true); } catch { }
    }
}
