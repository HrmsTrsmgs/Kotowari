using Marimo.Kotowari.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Benchmarks;

internal sealed class StackBufferWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    internal const int StackBufferLimit = 256;

    protected override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out string parsed)
    {
        var current = SkipWhiteSpace(start);
        Span<char> returnValue = Parsers.Length <= StackBufferLimit
            ? stackalloc char[Parsers.Length]
            : new char[Parsers.Length];
        var index = 0;
        foreach (var parser in Parsers)
        {
            if (!TryParseChild(parser, current, out var childNext, out var childParsed))
            {
                next = start;
                parsed = default;
                return false;
            }

            current = childNext;
            returnValue[index++] = childParsed;
        }

        current = SkipWhiteSpace(current);
        next = current;
        parsed = new string(returnValue);
        return true;
    }
}
