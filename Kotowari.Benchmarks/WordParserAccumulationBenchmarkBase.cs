using BenchmarkDotNet.Attributes;
using Marimo.Kotowari.Core;
using System;
using System.Linq;

namespace Marimo.Kotowari.Benchmarks;

public abstract class WordParserAccumulationBenchmarkBase
{
    WordParserAccumulationParser[] parsers = [];
    string text = "";

    public abstract WordParserAccumulationCase Case { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (word, text, succeeds) = WordParserAccumulationInputs.Get(Case);
        this.text = text;
        var cursol = new Cursol(text);
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
    // Unlike the saved-cursor measurements, every call includes cursor creation and its input-state allocation.
    [Benchmark(Baseline = true)]
    public ParseResult<string> List()
        => parsers[(int)WordParserAccumulationStrategy.List].Parse(new Cursol(text));

    [Benchmark]
    public ParseResult<string> ListCapacity()
        => parsers[(int)WordParserAccumulationStrategy.ListCapacity].Parse(new Cursol(text));

    [Benchmark]
    public ParseResult<string> StringBuilder()
        => parsers[(int)WordParserAccumulationStrategy.StringBuilder].Parse(new Cursol(text));

    [Benchmark]
    public ParseResult<string> StringBuilderCapacity()
        => parsers[(int)WordParserAccumulationStrategy.StringBuilderCapacity].Parse(new Cursol(text));

    [Benchmark]
    public ParseResult<string> Array()
        => parsers[(int)WordParserAccumulationStrategy.Array].Parse(new Cursol(text));

    [Benchmark]
    public ParseResult<string> DeferredArray()
        => parsers[(int)WordParserAccumulationStrategy.DeferredArray].Parse(new Cursol(text));

    [Benchmark]
    public ParseResult<string> StackBuffer()
        => parsers[(int)WordParserAccumulationStrategy.StackBuffer].Parse(new Cursol(text));
}
