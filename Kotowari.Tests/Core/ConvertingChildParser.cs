using Marimo.Kotowari.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Tests.Core;

internal sealed class ConvertingChildParser<TChild, T>(Parser<TChild> parser, Func<TChild, T> convert) : Parser<T>
{
    protected override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
    {
        if (!TryParseChild(parser, start, out next, out var childParsed))
        {
            parsed = default;
            return false;
        }

        parsed = convert(childParsed);
        return true;
    }
}
