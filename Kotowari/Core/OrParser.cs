using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OrParser<T> : Parser<T>
{
    Parser<T>[] Parsers { get; }

    public OrParser(params Parser<T>[] parsers)
        => Parsers = parsers;

    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
    {
        foreach (var parser in Parsers)
        {
            if (parser.TryParseCore(start, out next, out parsed))
            {
                return true;
            }
        }
        next = start;
        parsed = default;
        return false;
    }
}
