using System.IO;
using Bogus;
using Tamp;
using Xunit;

namespace Tamp.GraphQLCodegen.V5.Tests;

public sealed class GraphQLCodegenTests
{
    private static Tool FakeTool(string name = "graphql-codegen")
    {
        // Path.GetFullPath rewrites POSIX paths to drive-rooted on Windows
        // — comparing through tool.Executable.Value keeps the tests
        // cross-platform.
        return new Tool(AbsolutePath.Create(Path.Combine(Path.GetTempPath(), name)));
    }

    private static int IndexOf(IReadOnlyList<string> args, string value, int start = 0)
    {
        for (var i = start; i < args.Count; i++)
            if (args[i] == value) return i;
        return -1;
    }

    [Fact]
    public void Every_Verb_Uses_Tool_Path_As_Executable()
    {
        var tool = FakeTool();
        Assert.Equal(tool.Executable.Value, GraphQLCodegen.Generate(tool).Executable);
        Assert.Equal(tool.Executable.Value, GraphQLCodegen.Init(tool).Executable);
        Assert.Equal(tool.Executable.Value, GraphQLCodegen.Raw(tool, "--help").Executable);
    }

    [Fact]
    public void Generate_With_No_Configuration_Emits_Empty_Args()
    {
        var plan = GraphQLCodegen.Generate(FakeTool());
        Assert.Empty(plan.Arguments);
    }

    [Fact]
    public void Generate_Config_Round_Trips()
    {
        var plan = GraphQLCodegen.Generate(FakeTool(), s => s.SetConfig("codegen.ts"));
        var args = plan.Arguments;
        Assert.Equal("--config", args[0]);
        Assert.Equal("codegen.ts", args[1]);
    }

    [Fact]
    public void Generate_Overwrite_Is_A_Single_Flag()
    {
        var plan = GraphQLCodegen.Generate(FakeTool(), s => s.SetOverwrite());
        Assert.Contains("--overwrite", plan.Arguments);
    }

    [Fact]
    public void Generate_Watch_Without_Pattern()
    {
        var plan = GraphQLCodegen.Generate(FakeTool(), s => s.SetWatch());
        Assert.Contains("--watch", plan.Arguments);
        Assert.Single(plan.Arguments, "--watch");
    }

    [Fact]
    public void Generate_Watch_With_Pattern_Emits_Pattern_After_Flag()
    {
        var plan = GraphQLCodegen.Generate(FakeTool(), s => s.SetWatch(true, "src/**/*.graphql"));
        var args = plan.Arguments;
        var watchIdx = IndexOf(args, "--watch");
        Assert.True(watchIdx >= 0);
        Assert.Equal("src/**/*.graphql", args[watchIdx + 1]);
    }

    [Fact]
    public void Generate_Project_Round_Trips()
    {
        var plan = GraphQLCodegen.Generate(FakeTool(), s => s.SetProject("server"));
        var args = plan.Arguments;
        var idx = IndexOf(args, "--project");
        Assert.True(idx >= 0);
        Assert.Equal("server", args[idx + 1]);
    }

    [Fact]
    public void Generate_Common_Flags_All_Round_Trip()
    {
        var plan = GraphQLCodegen.Generate(FakeTool(), s => s
            .SetSilent()
            .SetVerbose()
            .SetDebug()
            .SetErrorsOnly()
            .SetProfile()
            .AddRequire("dotenv/config")
            .AddRequire("ts-node/register"));
        var args = plan.Arguments;
        Assert.Contains("--silent", args);
        Assert.Contains("--verbose", args);
        Assert.Contains("--debug", args);
        Assert.Contains("--errors-only", args);
        Assert.Contains("--profile", args);

        // --require pairs preserve order and emit two flag pairs.
        var first = IndexOf(args, "--require");
        var second = IndexOf(args, "--require", first + 1);
        Assert.True(first >= 0 && second > first);
        Assert.Equal("dotenv/config", args[first + 1]);
        Assert.Equal("ts-node/register", args[second + 1]);
    }

    [Fact]
    public void Generate_Config_Comes_Before_Common_Flags()
    {
        var plan = GraphQLCodegen.Generate(FakeTool(), s => s
            .SetConfig("codegen.yml")
            .SetVerbose());
        var args = plan.Arguments;
        var configIdx = IndexOf(args, "--config");
        var verboseIdx = IndexOf(args, "--verbose");
        Assert.True(configIdx >= 0 && verboseIdx >= 0);
        Assert.True(configIdx < verboseIdx);
    }

    [Fact]
    public void Generate_Working_Directory_Flows_To_Plan()
    {
        var cwd = Path.GetTempPath();
        var plan = GraphQLCodegen.Generate(FakeTool(), s => s.SetWorkingDirectory(cwd));
        Assert.Equal(cwd, plan.WorkingDirectory);
    }

    [Fact]
    public void Generate_Env_Vars_Flow_To_Plan()
    {
        var plan = GraphQLCodegen.Generate(FakeTool(), s => s.SetEnv("NODE_OPTIONS", "--max-old-space-size=4096"));
        Assert.Equal("--max-old-space-size=4096", plan.Environment["NODE_OPTIONS"]);
    }

    [Fact]
    public void Init_Begins_With_init_Verb()
    {
        var plan = GraphQLCodegen.Init(FakeTool());
        Assert.Equal("init", plan.Arguments[0]);
    }

    [Fact]
    public void Init_Common_Flags_Tail_The_Verb()
    {
        var plan = GraphQLCodegen.Init(FakeTool(), s => s.SetVerbose());
        Assert.Equal("init", plan.Arguments[0]);
        Assert.Contains("--verbose", plan.Arguments);
    }

    [Fact]
    public void Raw_Requires_At_Least_One_Argument()
    {
        Assert.Throws<ArgumentException>(() => GraphQLCodegen.Raw(FakeTool()));
    }

    [Fact]
    public void Raw_Forwards_Arguments_Verbatim()
    {
        var plan = GraphQLCodegen.Raw(FakeTool(), "--config", "x.yml", "--version");
        Assert.Equal(["--config", "x.yml", "--version"], plan.Arguments);
    }

    [Fact]
    public void Generate_Null_Tool_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GraphQLCodegen.Generate(null!));
    }

    [Fact]
    public void Init_Null_Tool_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GraphQLCodegen.Init(null!));
    }

    [Fact]
    public void Raw_Null_Tool_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GraphQLCodegen.Raw(null!, "--help"));
    }

    [Fact]
    public void Generate_Many_Requires_Preserves_Order_Under_Random_Names()
    {
        // Bogus-generated module names exercise the "repeat --require with
        // arbitrary text" path. Order preservation matters because
        // codegen's preload order is observable (later modules can shadow
        // earlier ones).
        var faker = new Faker();
        var modules = Enumerable.Range(0, 8).Select(_ => faker.System.FileName(".js")).ToArray();

        var plan = GraphQLCodegen.Generate(FakeTool(), s =>
        {
            foreach (var m in modules) s.AddRequire(m);
        });

        // Walk the args extracting --require pairs in order.
        var observed = new List<string>();
        for (var i = 0; i < plan.Arguments.Count - 1; i++)
            if (plan.Arguments[i] == "--require") observed.Add(plan.Arguments[i + 1]);

        Assert.Equal(modules, observed);
    }
}
