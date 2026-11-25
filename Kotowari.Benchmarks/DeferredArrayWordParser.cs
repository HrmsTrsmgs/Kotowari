using Marimo.Kotowari.Core;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Benchmarks;

internal sealed class DeferredArrayWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out string parsed)
    {
        var current = SkipWhiteSpace(start);
        char[]? returnValue = null;
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
            returnValue ??= new char[Parsers.Length];
            returnValue[index++] = childParsed;
        }

        current = SkipWhiteSpace(current);
        next = current;
        parsed = returnValue is null ? string.Empty : new string(returnValue);
        return true;
    }
}
