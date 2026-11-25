using Marimo.Kotowari.Core;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Benchmarks;

// Accumulation matches HEAD 15dbfed's List baseline; child parsing follows the current bool/out protocol.
internal sealed class ListWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out string parsed)
    {
        var current = SkipWhiteSpace(start);

        var returnValue = new List<char>();
        foreach (var parser in Parsers)
        {
            if (!TryParseChild(parser, current, out var childNext, out var childParsed))
            {
                next = start;
                parsed = default;
                return false;
            }

            current = childNext;
            returnValue.Add(childParsed);
        }
        current = SkipWhiteSpace(current);
        next = current;
        parsed = new string([.. returnValue]);
        return true;
    }
}
