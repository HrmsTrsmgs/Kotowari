using BenchmarkDotNet.Attributes;
using Marimo.Kotowari.Core;
using System;

namespace Marimo.Kotowari.Benchmarks;

[MemoryDiagnoser]
public class WordParserBenchmarks
{
    const string Word = "false";
    readonly WordParser wordParser = new(Word, true);

    [Params("false,", "0,", "falsX,")]
    public string Text { get; set; } = "";

    [GlobalSetup]
    public void Setup()
    {
        var cursol = Cursol.FromString(Text);
        var result = wordParser.Parse(cursol);
        var succeeds = Text == "false,";
        if (result.IsSuccess != succeeds
            || result.Cursol.Index != (succeeds ? Word.Length : 0)
            || (succeeds && result.Parsed != Word))
        {
            throw new InvalidOperationException("WordParserの測定入力を解析できませんでした。");
        }
    }

    // Unlike the saved-cursor measurements, both methods include cursor creation and its input-state allocation.
    [Benchmark]
    public ParseResult<string, StringDocument> ParseReused()
        => wordParser.Parse(Cursol.FromString(Text));

    [Benchmark]
    public ParseResult<string, StringDocument> ConstructAndParse()
        => new WordParser(Word, true).Parse(Cursol.FromString(Text));
}
