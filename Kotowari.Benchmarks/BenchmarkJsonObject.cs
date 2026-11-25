using System.Collections.Generic;

namespace Marimo.Kotowari.Benchmarks;

internal sealed record BenchmarkJsonObject(IReadOnlyDictionary<string, BenchmarkJson> Members) : BenchmarkJson;
