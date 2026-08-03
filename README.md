# Kotowari

Kotowari is a dependency-free parser combinator library for C#.

## Requirements

The NuGet package targets `netstandard2.0` and `net10.0`. Tests and benchmarks use the .NET 10 SDK pinned in `global.json`.

## Build and test

```powershell
dotnet build Kotowari.sln -c Release
dotnet test Kotowari.sln -c Release
```

The test suites use xUnit v3 and Fluent Assertions. Fluent Assertions 8 is free for open-source and non-commercial use; commercial use requires a paid license. See the [Fluent Assertions license](https://fluentassertions.com/introduction#licensing).

## Benchmarks

Benchmarks are implemented with BenchmarkDotNet and include `System.Text.Json` as the platform baseline.

```powershell
dotnet run --project Kotowari.Benchmarks/Marimo.Kotowari.Benchmarks.csproj -c Release
```

## License

Kotowari is licensed under the Apache License 2.0.
