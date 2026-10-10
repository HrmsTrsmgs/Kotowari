using BenchmarkDotNet.Attributes;

namespace Marimo.Kotowari.Benchmarks;

[MemoryDiagnoser]
public class WordParserAccumulationBenchmarks : WordParserAccumulationBenchmarkBase
{
    [Params(WordParserAccumulationCase.False5Success,
        WordParserAccumulationCase.False5FirstFailure, WordParserAccumulationCase.False5LastFailure)]
    public override WordParserAccumulationCase Case { get; set; }
}
