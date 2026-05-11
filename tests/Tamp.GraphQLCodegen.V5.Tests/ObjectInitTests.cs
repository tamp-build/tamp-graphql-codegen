using System.IO;
using Tamp;
using Xunit;

namespace Tamp.GraphQLCodegen.V5.Tests;

/// <summary>
/// Object-init overload parity (TAM-161). Two authoring styles, identical CommandPlans.
/// </summary>
public sealed class ObjectInitTests
{
    private static Tool FakeTool(string name = "graphql-codegen")
        => new(AbsolutePath.Create(Path.Combine(Path.GetTempPath(), name)));

    private static int IndexOf(IReadOnlyList<string> args, string value, int start = 0)
    {
        for (var i = start; i < args.Count; i++)
            if (args[i] == value) return i;
        return -1;
    }

    [Fact]
    public void Generate_ObjectInit_Emits_Identical_Plan_To_Fluent()
    {
        var tool = FakeTool();
        var fluent = GraphQLCodegen.Generate(tool, s => s
            .SetConfig("codegen.ts")
            .SetOverwrite()
            .SetVerbose()
            .AddRequire("dotenv/config"));

        var objectInit = GraphQLCodegen.Generate(tool, new GraphQLCodegenGenerateSettings
        {
            Config = "codegen.ts",
            Overwrite = true,
            Verbose = true,
            Require = { "dotenv/config" },
        });

        Assert.Equal(fluent.Executable, objectInit.Executable);
        Assert.Equal(fluent.Arguments, objectInit.Arguments);
    }

    [Fact]
    public void Generate_ObjectInit_Watch_With_Pattern_Round_Trips()
    {
        var args = GraphQLCodegen.Generate(FakeTool(), new GraphQLCodegenGenerateSettings
        {
            Watch = true,
            WatchPattern = "src/**/*.graphql",
            Project = "server",
        }).Arguments;

        var watchIdx = IndexOf(args, "--watch");
        Assert.True(watchIdx >= 0);
        Assert.Equal("src/**/*.graphql", args[watchIdx + 1]);
        var projectIdx = IndexOf(args, "--project");
        Assert.True(projectIdx >= 0);
        Assert.Equal("server", args[projectIdx + 1]);
    }

    [Fact]
    public void Generate_ObjectInit_Working_Directory_And_Env_Flow_To_Plan()
    {
        var cwd = Path.GetTempPath();
        var plan = GraphQLCodegen.Generate(FakeTool(), new GraphQLCodegenGenerateSettings
        {
            WorkingDirectory = cwd,
            EnvironmentVariables = { ["NODE_OPTIONS"] = "--max-old-space-size=4096" },
        });
        Assert.Equal(cwd, plan.WorkingDirectory);
        Assert.Equal("--max-old-space-size=4096", plan.Environment["NODE_OPTIONS"]);
    }

    [Fact]
    public void Init_ObjectInit_Emits_Identical_Plan_To_Fluent()
    {
        var tool = FakeTool();
        var fluent = GraphQLCodegen.Init(tool, s => s.SetVerbose().SetDebug());
        var objectInit = GraphQLCodegen.Init(tool, new GraphQLCodegenInitSettings
        {
            Verbose = true,
            Debug = true,
        });

        Assert.Equal(fluent.Arguments, objectInit.Arguments);
    }

    [Fact]
    public void Init_ObjectInit_Begins_With_init_Verb()
    {
        var plan = GraphQLCodegen.Init(FakeTool(), new GraphQLCodegenInitSettings());
        Assert.Equal("init", plan.Arguments[0]);
    }

    [Fact]
    public void ObjectInit_Surface_Compiles_And_Returns_CommandPlan()
    {
        // Smoke test: each wrapper accepts an object-init settings argument and returns a non-null CommandPlan.
        var tool = FakeTool();
        Assert.NotNull(GraphQLCodegen.Generate(tool, new GraphQLCodegenGenerateSettings { Config = "codegen.yml" }));
        Assert.NotNull(GraphQLCodegen.Init(tool, new GraphQLCodegenInitSettings()));
    }

    [Fact]
    public void Generate_ObjectInit_Null_Tool_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GraphQLCodegen.Generate(null!, new GraphQLCodegenGenerateSettings()));
    }

    [Fact]
    public void Generate_ObjectInit_Null_Settings_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GraphQLCodegen.Generate(FakeTool(), (GraphQLCodegenGenerateSettings)null!));
    }

    [Fact]
    public void Init_ObjectInit_Null_Tool_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GraphQLCodegen.Init(null!, new GraphQLCodegenInitSettings()));
    }

    [Fact]
    public void Init_ObjectInit_Null_Settings_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GraphQLCodegen.Init(FakeTool(), (GraphQLCodegenInitSettings)null!));
    }
}
