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

### Compare two commits on GitHub Actions

In **Actions → Giant JSON benchmark → Run workflow**, enter the full 40-character SHA of the baseline and candidate. Leave the candidate empty to use the selected branch's commit, or enter the same SHA twice for an **A/A noise check**. Use trusted commits already pushed to this repository. The [workflow](.github/workflows/giant-json-benchmark.yml) is manual only; [GitHub enables manual dispatch after the workflow is on the default branch](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow).

One Windows runner builds both revisions in Release and checks the actual benchmark input against the recorded Windows sample (4,172,062 bytes, CRLF). Both revisions must consume all characters and produce identical JSON trees with the same benchmark harness and SDK. It then measures Kotowari's **parse only** (file I/O is outside timing), in four pairs of fresh processes. The order reverses each pair; Dynamic PGO stays on.

The run summary shows time and allocated bytes per parse for each pair, plus the paired time ratio and its approximate 95% interval. Compare **within the same run**: hosted machines can differ, so do not compare absolute times to the local records or to a different job. Download the artifact within 14 days for the SHAs, SDK/runtime, input and tree hashes, and raw BenchmarkDotNet reports. Nothing is automatically committed; if a result informs a decision, preserve it in the existing benchmark records before the artifact expires.

The suspended UTF-8 work, saved Git tags, and measurement baselines are indexed in the [UTF-8 experiment archive](docs/utf8-experiment-archive.md).

## Development continuity

Development decisions and existing specification sources are indexed in [Development context](docs/development-context.md) (Japanese). Current work and review candidates are indexed separately in [AI work notes](ai-notes/README.md). AI contributors should start with [AGENTS.md](AGENTS.md); the existing TDD workflow is unchanged.

### Compare two commits on GitHub Actions

In **Actions → Giant JSON benchmark → Run workflow**, enter the full 40-character SHA of the baseline and candidate. Leave the candidate empty to use the selected branch's commit, or enter the same SHA twice for an **A/A noise check**. Use trusted commits already pushed to this repository. The [workflow](.github/workflows/giant-json-benchmark.yml) is manual only; [GitHub enables manual dispatch after the workflow is on the default branch](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow).

One Windows runner builds both revisions in Release and checks the actual benchmark input against the recorded Windows sample (4,172,062 bytes, CRLF). Both revisions must consume all characters and produce identical JSON trees with the same benchmark harness and SDK. It then measures Kotowari's **parse only** (file I/O is outside timing), in four pairs of fresh processes. The order reverses each pair; Dynamic PGO stays on.

The run summary shows time and allocated bytes per parse for each pair, plus the paired time ratio and its approximate 95% interval. Compare **within the same run**: hosted machines can differ, so do not compare absolute times to the local records or to a different job. Download the artifact within 14 days for the SHAs, SDK/runtime, input and tree hashes, and raw BenchmarkDotNet reports. Nothing is automatically committed; if a result informs a decision, preserve it in the existing benchmark records before the artifact expires.


## License

Kotowari is licensed under the Apache License 2.0.
