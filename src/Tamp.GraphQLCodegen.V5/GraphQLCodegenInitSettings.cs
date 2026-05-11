namespace Tamp.GraphQLCodegen.V5;

/// <summary>
/// Settings for <c>graphql-codegen init</c> — the interactive scaffold
/// that walks the user through generating a <c>codegen.yml</c>. In CI
/// this is mostly useful as a smoke test; production runs use the
/// generate verb against a checked-in config.
/// </summary>
public sealed class GraphQLCodegenInitSettings : GraphQLCodegenSettingsBase
{
    protected override IEnumerable<string> BuildVerbArguments()
    {
        var args = new List<string> { "init" };
        EmitCommonArguments(args);
        return args;
    }
}
