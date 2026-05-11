namespace Tamp.GraphQLCodegen.V5;

/// <summary>Facade for graphql-code-generator 5.x verbs.</summary>
/// <remarks>
/// <para>Resolve via <c>[NuGetPackage(UseSystemPath = true)]</c> — typically installed globally or via the project's node_modules:</para>
/// <code>
/// [NuGetPackage("graphql-codegen", UseSystemPath = true)]
/// readonly Tool Codegen;
/// </code>
/// </remarks>
public static class GraphQLCodegen
{
    /// <summary><c>graphql-codegen [flags]</c> — the default generate invocation.</summary>
    public static CommandPlan Generate(Tool tool, Action<GraphQLCodegenGenerateSettings>? configure = null)
        => Build<GraphQLCodegenGenerateSettings>(tool, configure);

    /// <summary><c>graphql-codegen init</c> — interactive scaffold.</summary>
    public static CommandPlan Init(Tool tool, Action<GraphQLCodegenInitSettings>? configure = null)
        => Build<GraphQLCodegenInitSettings>(tool, configure);

    /// <summary>Escape hatch for arbitrary CLI invocations we haven't typed.</summary>
    public static CommandPlan Raw(Tool tool, params string[] arguments)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (arguments is null || arguments.Length == 0)
            throw new ArgumentException("Raw requires at least one argument.", nameof(arguments));
        var s = new GraphQLCodegenRawSettings();
        s.AddArgs(arguments);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan Build<T>(Tool tool, Action<T>? configure) where T : GraphQLCodegenSettingsBase, new()
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        var s = new T();
        configure?.Invoke(s);
        return s.ToCommandPlan(tool);
    }
}
