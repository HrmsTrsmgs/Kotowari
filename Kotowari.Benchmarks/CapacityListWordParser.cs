using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Benchmarks;

internal sealed class CapacityListWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out string parsed)
    {
        var current = SkipBlankAsync(start);
        var returnValue = new List<char>(Parsers.Length);
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

        current = SkipBlankAsync(current);
        next = current;
        parsed = new string([.. returnValue]);
        return true;
    }
}
