using BenchmarkDotNet.Attributes;
using Marimo.Kotowari.Core;
using System;
using System.Linq;

namespace Marimo.Kotowari.Benchmarks;


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
