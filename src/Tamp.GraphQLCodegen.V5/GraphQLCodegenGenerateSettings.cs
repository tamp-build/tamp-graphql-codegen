namespace Tamp.GraphQLCodegen.V5;

/// <summary>
/// Settings for the default <c>graphql-codegen</c> generate invocation
/// — no subcommand, just flags. Drives generation from a codegen
/// config (codegen.yml/ts/js) and produces one or more output files.
/// </summary>
public sealed class GraphQLCodegenGenerateSettings : GraphQLCodegenSettingsBase
{
    /// <summary>Path to the codegen config file. Maps to <c>--config</c>.</summary>
    public string? Config { get; set; }

    /// <summary>Overwrite existing output files. Maps to <c>--overwrite</c>.</summary>
    public bool Overwrite { get; set; }

    /// <summary>Watch mode. When the value is non-null, the glob is appended to <c>--watch</c>.</summary>
    public bool Watch { get; set; }

    /// <summary>Optional glob pattern passed alongside <c>--watch</c>.</summary>
    public string? WatchPattern { get; set; }

    /// <summary>Project name for monorepo configs. Maps to <c>--project</c>.</summary>
    public string? Project { get; set; }

    public new GraphQLCodegenGenerateSettings SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }
    public GraphQLCodegenGenerateSettings SetConfig(string? path) { Config = path; return this; }
    public GraphQLCodegenGenerateSettings SetOverwrite(bool v = true) { Overwrite = v; return this; }
    public GraphQLCodegenGenerateSettings SetWatch(bool v = true, string? pattern = null) { Watch = v; WatchPattern = pattern; return this; }
    public GraphQLCodegenGenerateSettings SetProject(string? name) { Project = name; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        // generate is the implicit default — no verb token.
        var args = new List<string>();
        if (!string.IsNullOrEmpty(Config)) { args.Add("--config"); args.Add(Config!); }
        if (Overwrite) args.Add("--overwrite");
        if (Watch)
        {
            if (!string.IsNullOrEmpty(WatchPattern))
            {
                args.Add("--watch");
                args.Add(WatchPattern!);
            }
            else
            {
                args.Add("--watch");
            }
        }
        if (!string.IsNullOrEmpty(Project)) { args.Add("--project"); args.Add(Project!); }
        EmitCommonArguments(args);
        return args;
    }
}
