using Marimo.Kotowari.Core;
using System;
using System.Linq;

namespace Marimo.Kotowari.Benchmarks;

// Only construction and whitespace handling are shared; accumulation is measured independently.
internal abstract class WordParserAccumulationParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : Parser<string>
{
    protected Parser<char>[] Parsers { get; } = [.. word.Select(c => new CharParser(c, ignoreCase))];
    Parser<char> WhiteSpace { get; } = whiteSpace ?? throw new ArgumentNullException(nameof(whiteSpace));

    protected Cursol SkipWhiteSpace(Cursol current)
    {
        while (true)
        {
            if (!TryParseChild(WhiteSpace, current, out var next, out _)) return current;

            current = next;
        }
    }
}
