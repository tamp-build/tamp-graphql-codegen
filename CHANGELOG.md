# Changelog

All notable changes to this satellite are documented here. Format roughly follows Keep a Changelog; versions track `Directory.Build.props`.

## [Unreleased]

### Added

- Package now ships XML documentation files (`.xml`) alongside the assembly, so consumers get IntelliSense and API docs. (Mirrors [tamp-build/tamp#3](https://github.com/tamp-build/tamp/pull/50).)


## [0.1.1] - 2026-05-11

### Added
- Object-init overloads on every GraphQL Codegen wrapper (TAM-161 satellite fanout).

## [0.1.0]

- Initial Tamp.GraphQLCodegen.V5 facade — `Generate`, `Init`, `Raw` over graphql-code-generator 5.x.
