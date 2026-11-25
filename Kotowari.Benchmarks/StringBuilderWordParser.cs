using Marimo.Kotowari.Core;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Marimo.Kotowari.Benchmarks;

internal sealed class StringBuilderWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out string parsed)
    {
        var current = SkipWhiteSpace(start);
        var returnValue = new StringBuilder();
        foreach (var parser in Parsers)
        {
            if (!TryParseChild(parser, current, out var childNext, out var childParsed))
            {
                next = start;
                parsed = default;
                return false;
            }

            current = childNext;
            returnValue.Append(childParsed);
        }

        current = SkipWhiteSpace(current);
        next = current;
        parsed = returnValue.ToString();
        return true;
    }
}
