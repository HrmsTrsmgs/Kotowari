using BenchmarkDotNet.Attributes;
using Marimo.Kotowari.Core;
using System;
using System.Linq;

namespace Marimo.Kotowari.Benchmarks;

public enum WordParserAccumulationCase
{
    False5Success,
    False5FirstFailure,
    False5LastFailure,
    True4Success,
    True4FirstFailure,
    True4LastFailure,
    Long32Success,
    Long32FirstFailure,
    Long32LastFailure,
    Long128Success,
    Long128FirstFailure,
    Long128LastFailure
}

internal static class WordParserAccumulationInputs
{
    internal static (string Word, string Text, bool Succeeds) Get(WordParserAccumulationCase @case)
    {
        var word = @case switch
        {
            <= WordParserAccumulationCase.False5LastFailure => "false",
            <= WordParserAccumulationCase.True4LastFailure => "true",
            <= WordParserAccumulationCase.Long32LastFailure => new string('a', 31) + "z",
            <= WordParserAccumulationCase.Long128LastFailure => new string('a', 127) + "z",
            _ => throw new ArgumentOutOfRangeException(nameof(@case))
        };
        return ((int)@case % 3) switch
        {
            0 => (word, word + ",", true),
            1 => (word, "!" + word[1..] + ",", false),
            _ => (word, word[..^1] + "!,", false)
        };
    }
}

public abstract class WordParserAccumulationBenchmarkBase
{
    WordParserAccumulationParser[] parsers = [];
    Cursol cursol;

    public abstract WordParserAccumulationCase Case { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (word, text, succeeds) = WordParserAccumulationInputs.Get(Case);
        cursol = new Cursol(text);
        parsers = [.. Enum.GetValues<WordParserAccumulationStrategy>()
            .Select(it => WordParserAccumulationCandidates.Create(it, word, true))];

        var expected = succeeds ? cursol.GoFoward(word.Length) : cursol;
        foreach (var parser in parsers)
        {
            var result = parser.Parse(cursol);
            if (result.IsSuccess != succeeds || result.Cursol != expected
                || (succeeds && result.Parsed != word))
            {
                throw new InvalidOperationException($"{parser.GetType().Name}: {Case} の解析結果が一致しません。");
            }
        }
    }

    // All candidates have the same array lookup and Parser<string>.Parse dispatch.
    [Benchmark(Baseline = true)]
    public ParseResult<string> List() => parsers[(int)WordParserAccumulationStrategy.List].Parse(cursol);

    [Benchmark]
    public ParseResult<string> ListCapacity() => parsers[(int)WordParserAccumulationStrategy.ListCapacity].Parse(cursol);

    [Benchmark]
    public ParseResult<string> StringBuilder() => parsers[(int)WordParserAccumulationStrategy.StringBuilder].Parse(cursol);

    [Benchmark]
    public ParseResult<string> StringBuilderCapacity() => parsers[(int)WordParserAccumulationStrategy.StringBuilderCapacity].Parse(cursol);

    [Benchmark]
    public ParseResult<string> IteratorConcat() => parsers[(int)WordParserAccumulationStrategy.IteratorConcat].Parse(cursol);

    [Benchmark]
    public ParseResult<string> IteratorArray() => parsers[(int)WordParserAccumulationStrategy.IteratorArray].Parse(cursol);

    [Benchmark]
    public ParseResult<string> ResultLambda() => parsers[(int)WordParserAccumulationStrategy.ResultLambda].Parse(cursol);

    [Benchmark]
    public ParseResult<string> Array() => parsers[(int)WordParserAccumulationStrategy.Array].Parse(cursol);

    [Benchmark]
    public ParseResult<string> DeferredArray() => parsers[(int)WordParserAccumulationStrategy.DeferredArray].Parse(cursol);

    [Benchmark]
    public ParseResult<string> StackBuffer() => parsers[(int)WordParserAccumulationStrategy.StackBuffer].Parse(cursol);
}

[MemoryDiagnoser]
public class WordParserAccumulationBenchmarks : WordParserAccumulationBenchmarkBase
{
    [Params(WordParserAccumulationCase.False5Success,
        WordParserAccumulationCase.False5FirstFailure, WordParserAccumulationCase.False5LastFailure)]
    public override WordParserAccumulationCase Case { get; set; }
}

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
