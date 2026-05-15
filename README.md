# Tamp.GraphQLCodegen

[graphql-code-generator](https://the-guild.dev/graphql/codegen) CLI
wrapper for [Tamp](https://github.com/tamp-build/tamp).

| Package | graphql-codegen | Status |
|---|---|---|
| [`Tamp.GraphQLCodegen.V5`](src/Tamp.GraphQLCodegen.V5) | 5.x | preview |

Requires `Tamp.Core ≥ 1.0.3`.

## Why a separate repo

graphql-code-generator's CLI surface is small but its plugin ecosystem
shifts independently. Per the satellite-repo convention, this tracks
the codegen CLI's release cadence independently of `tamp` core.

## Install

```xml
<PackageVersion Include="Tamp.GraphQLCodegen.V5" Version="0.1.0" />
```

```xml
<PackageReference Include="Tamp.GraphQLCodegen.V5" />
```

## Verbs

| Verb | Notes |
|---|---|
| `Generate` | The default invocation. `--config`, `--overwrite`, `--watch [pattern]`, `--project`. |
| `Init` | Interactive scaffold. |
| `Raw` | Escape hatch for flag combinations we haven't typed. |

Common flags (apply to every verb): `--silent`, `--verbose`,
`--debug`, `--errors-only`, `--profile`, `--require <module>`
(repeatable).

## Quick example

```csharp
using Tamp;
using Tamp.GraphQLCodegen.V5;

[NuGetPackage("graphql-codegen", UseSystemPath = true)]
readonly Tool Codegen = null!;

Target Codegen => _ => _.Executes(() =>
    GraphQLCodegen.Generate(Codegen, s => s
        .SetConfig("codegen.ts")
        .SetOverwrite()
        .SetWorkingDirectory(RootDirectory / "frontend")));
```

## Test the wrapper itself

The integration tests stage a small schema + operation fixture in a
temp dir and run `graphql-codegen --config codegen.yml --silent`
against it. They expect `graphql-codegen` on PATH:

```bash
npm install -g @graphql-codegen/cli@5 graphql@16
```

Unit tests don't need any external tool — they verify CLI argument
shape only.

## Releasing

See [MAINTAINERS.md](MAINTAINERS.md).

## Settings authoring style

Examples above use the fluent `Set*`-chain shape. Every wrapper verb also accepts a `new XxxSettings { ... }` object-init form — both produce identical `CommandPlan`s. The fluent shape stays canonical in docs and the `tamp init` template; opt into object-init scaffolding via `tamp init --settings-style=init`.

See [Build Script Authoring → Two authoring styles](https://github.com/tamp-build/tamp/wiki/Build-Script-Authoring#two-authoring-styles-for-wrapper-calls-120) on the wiki for the side-by-side comparison.
