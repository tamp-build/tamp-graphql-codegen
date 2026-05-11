namespace Tamp.GraphQLCodegen.V5;

/// <summary>
/// Common base for graphql-code-generator 5.x verbs. Holds the shared
/// flags (verbose, silent, profile, errors-only) plus working directory
/// and environment variables.
/// </summary>
public abstract class GraphQLCodegenSettingsBase
{
    /// <summary>Working directory for the child process (where node looks for the config file).</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Environment variables merged into the process env block.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>Suppresses all output. Maps to <c>--silent</c> / <c>-s</c>.</summary>
    public bool Silent { get; set; }

    /// <summary>Verbose logging. Maps to <c>--verbose</c> / <c>-v</c>.</summary>
    public bool Verbose { get; set; }

    /// <summary>Debug logging. Maps to <c>--debug</c>.</summary>
    public bool Debug { get; set; }

    /// <summary>Only emit errors (suppress info logs). Maps to <c>--errors-only</c>.</summary>
    public bool ErrorsOnly { get; set; }

    /// <summary>Emit a timing profile for each plugin. Maps to <c>--profile</c>.</summary>
    public bool Profile { get; set; }

    /// <summary>Preload one or more node modules before running. Repeated as <c>--require &lt;module&gt;</c>.</summary>
    public List<string> Require { get; } = [];

    public GraphQLCodegenSettingsBase SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }
    public GraphQLCodegenSettingsBase SetEnv(string key, string value) { EnvironmentVariables[key] = value; return this; }
    public GraphQLCodegenSettingsBase SetSilent(bool v = true) { Silent = v; return this; }
    public GraphQLCodegenSettingsBase SetVerbose(bool v = true) { Verbose = v; return this; }
    public GraphQLCodegenSettingsBase SetDebug(bool v = true) { Debug = v; return this; }
    public GraphQLCodegenSettingsBase SetErrorsOnly(bool v = true) { ErrorsOnly = v; return this; }
    public GraphQLCodegenSettingsBase SetProfile(bool v = true) { Profile = v; return this; }
    public GraphQLCodegenSettingsBase AddRequire(string module) { Require.Add(module); return this; }

    /// <summary>Subclasses build their per-verb argument list (verb token first, then flags).</summary>
    protected abstract IEnumerable<string> BuildVerbArguments();

    /// <summary>Subclasses override to expose typed Secrets to the redaction table. Default: none.</summary>
    protected virtual IReadOnlyList<Secret> CollectSecrets() => Array.Empty<Secret>();

    protected void EmitCommonArguments(List<string> args)
    {
        if (Silent) args.Add("--silent");
        if (Verbose) args.Add("--verbose");
        if (Debug) args.Add("--debug");
        if (ErrorsOnly) args.Add("--errors-only");
        if (Profile) args.Add("--profile");
        foreach (var m in Require) { args.Add("--require"); args.Add(m); }
    }

    public CommandPlan ToCommandPlan(Tool tool)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        var args = BuildVerbArguments().ToList();
        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = args,
            Environment = new Dictionary<string, string>(EnvironmentVariables),
            WorkingDirectory = WorkingDirectory,
            Secrets = CollectSecrets(),
        };
    }
}
