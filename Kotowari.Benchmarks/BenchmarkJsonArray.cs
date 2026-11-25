using System.Collections.Generic;

namespace Marimo.Kotowari.Benchmarks;

internal sealed record BenchmarkJsonArray(IReadOnlyList<BenchmarkJson> Elements) : BenchmarkJson;
