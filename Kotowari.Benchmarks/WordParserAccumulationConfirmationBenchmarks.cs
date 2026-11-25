using BenchmarkDotNet.Attributes;

namespace Marimo.Kotowari.Benchmarks;

[MemoryDiagnoser]
public class WordParserAccumulationConfirmationBenchmarks : WordParserAccumulationBenchmarkBase
{
    [Params(WordParserAccumulationCase.True4Success,
        WordParserAccumulationCase.True4FirstFailure, WordParserAccumulationCase.True4LastFailure,
        WordParserAccumulationCase.Long32Success,
        WordParserAccumulationCase.Long32FirstFailure, WordParserAccumulationCase.Long32LastFailure,
        WordParserAccumulationCase.Long128Success,
        WordParserAccumulationCase.Long128FirstFailure, WordParserAccumulationCase.Long128LastFailure)]
    public override WordParserAccumulationCase Case { get; set; }
}
