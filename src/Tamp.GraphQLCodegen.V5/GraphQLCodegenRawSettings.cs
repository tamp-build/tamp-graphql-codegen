namespace Tamp.GraphQLCodegen.V5;

/// <summary>
/// Escape hatch for graphql-codegen flag combinations we haven't typed
/// yet. Pass raw argv tokens; nothing is interpreted.
/// </summary>
public sealed class GraphQLCodegenRawSettings : GraphQLCodegenSettingsBase
{
    public List<string> RawArguments { get; } = [];

    public GraphQLCodegenRawSettings AddArgs(params string[] args) { RawArguments.AddRange(args); return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        // No common-flag emission — Raw is fully literal; the caller
        // owns every token. This is by design (mirrors Tamp.Turbo.V2).
        return RawArguments;
    }
}
