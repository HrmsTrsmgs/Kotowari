using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ParserConverter<U, T> : Parser<T>
{
    Parser<U> Parser { get; }
    Func<U, T> Converter { get; }

    public ParserConverter(Parser<U> parser, Func<U, T> converter)
    {
        Parser = parser;
        Converter = converter;
    }

    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
    {
        if (!Parser.TryParseCore(start, out next, out var childParsed))
        {
            parsed = default;
            return false;
        }

        parsed = Converter(childParsed);
        return true;
    }
}
