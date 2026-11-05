# Kotowari

Kotowari is a dependency-free parser combinator library for C#.

## Requirements

The NuGet package targets `net10.0`. Tests and benchmarks use the .NET 10 SDK pinned in `global.json`.

## Build and test

```powershell
dotnet build Kotowari.sln -c Release
dotnet test Kotowari.sln -c Release
```

The test suites use xUnit v3 and Fluent Assertions. Fluent Assertions 8 is free for open-source and non-commercial use; commercial use requires a paid license. See the [Fluent Assertions license](https://fluentassertions.com/introduction#licensing).

## Benchmarks

Benchmarks parse the bundled 4 MB JSON sample with Kotowari, Pidgin, Sprache, Superpower, and the legacy SpracheJSON package used by the original benchmark. Kotowari is the BenchmarkDotNet baseline; `System.Text.Json` is included separately as a reference for a dedicated JSON parser.

Validate that every parser can consume the sample before measuring:

```powershell
dotnet run --project Kotowari.Benchmarks/Marimo.Kotowari.Benchmarks.csproj -c Release -- --validate
```

Run the benchmark:

```powershell
dotnet run --project Kotowari.Benchmarks/Marimo.Kotowari.Benchmarks.csproj -c Release
```

Versioned benchmark records are listed in [`Kotowari.Benchmarks/Results`](Kotowari.Benchmarks/Results/README.md). Each record includes the input hash, environment, dependency versions, results, and known limitations.

The suspended UTF-8 work, saved Git tags, and measurement baselines are indexed in the [UTF-8 experiment archive](docs/utf8-experiment-archive.md).

## Development continuity

Development decisions and existing specification sources are indexed in [Development context](docs/development-context.md) (Japanese). Current work and review candidates are indexed separately in [AI work notes](ai-notes/README.md). AI contributors should start with [AGENTS.md](AGENTS.md); the existing TDD workflow is unchanged.

## License

Kotowari is licensed under the Apache License 2.0.
