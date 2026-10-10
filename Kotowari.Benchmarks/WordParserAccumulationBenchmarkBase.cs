using BenchmarkDotNet.Attributes;
using Marimo.Kotowari.Core;
using System;
using System.Linq;

namespace Marimo.Kotowari.Benchmarks;


public abstract class WordParserAccumulationBenchmarkBase
{
    WordParserAccumulationParser[] parsers = [];
    Cursol<StringDocument> cursol;

    public abstract WordParserAccumulationCase Case { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (word, text, succeeds) = WordParserAccumulationInputs.Get(Case);
        cursol = Cursol.FromString(text);
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
    public ParseResult<string, StringDocument> List() => parsers[(int)WordParserAccumulationStrategy.List].Parse(cursol);

    [Benchmark]
    public ParseResult<string, StringDocument> ListCapacity() => parsers[(int)WordParserAccumulationStrategy.ListCapacity].Parse(cursol);

    [Benchmark]
    public ParseResult<string, StringDocument> StringBuilder() => parsers[(int)WordParserAccumulationStrategy.StringBuilder].Parse(cursol);

    [Benchmark]
    public ParseResult<string, StringDocument> StringBuilderCapacity() => parsers[(int)WordParserAccumulationStrategy.StringBuilderCapacity].Parse(cursol);

    [Benchmark]
    public ParseResult<string, StringDocument> IteratorConcat() => parsers[(int)WordParserAccumulationStrategy.IteratorConcat].Parse(cursol);

    [Benchmark]
    public ParseResult<string, StringDocument> IteratorArray() => parsers[(int)WordParserAccumulationStrategy.IteratorArray].Parse(cursol);

    [Benchmark]
    public ParseResult<string, StringDocument> ResultLambda() => parsers[(int)WordParserAccumulationStrategy.ResultLambda].Parse(cursol);

    [Benchmark]
    public ParseResult<string, StringDocument> Array() => parsers[(int)WordParserAccumulationStrategy.Array].Parse(cursol);

    [Benchmark]
    public ParseResult<string, StringDocument> DeferredArray() => parsers[(int)WordParserAccumulationStrategy.DeferredArray].Parse(cursol);

    [Benchmark]
    public ParseResult<string, StringDocument> StackBuffer() => parsers[(int)WordParserAccumulationStrategy.StackBuffer].Parse(cursol);
}
