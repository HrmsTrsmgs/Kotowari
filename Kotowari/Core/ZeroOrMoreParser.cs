using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ZeroOrMoreParser<T> : Parser<IEnumerable<T>>
    where T : notnull
{
    Parser<T> Parser { get; }

    public ZeroOrMoreParser(Parser<T> parser)
        => Parser = parser;

    protected override ParseResult<IEnumerable<T>> ParseCore(Cursol cursol)
    {
        var parseds = new List<T> { };
        var current = cursol;
        while (true)
        {
            var result = Parser.Parse(current);
            if (!result.IsSuccess)
            {
                return ParseResult<IEnumerable<T>>.Success(result.Cursol, parseds);
            }

            current = result.Cursol;
            parseds.Add(result.Parsed);
        }
    }
}
