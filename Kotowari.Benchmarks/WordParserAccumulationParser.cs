using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Benchmarks;

// Only construction and whitespace handling are shared; accumulation is measured independently.
internal abstract class WordParserAccumulationParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : Parser<string>
{
    protected Parser<char>[] Parsers { get; } = [.. word.Select(c => new CharParser(c, ignoreCase))];
    public bool IgnoreCase { get; }
    Parser<char> WhiteSpace { get; } = whiteSpace ?? throw new ArgumentNullException(nameof(whiteSpace));

    protected Cursol SkipBlankAsync(Cursol current)
    {
        while (true)
        {
            if (!TryParseChild(WhiteSpace, current, out var next, out _)) return current;

            current = next;
        }
    }
}
