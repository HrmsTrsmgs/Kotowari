using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin;
using Sprache;
using Superpower;
using Superpower.Parsers;

namespace Marimo.Kotowari.Benchmarks;

internal sealed record BenchmarkJsonArray(IReadOnlyList<BenchmarkJson> Elements) : BenchmarkJson;
