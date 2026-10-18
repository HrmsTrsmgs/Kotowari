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

    protected internal override ParseResult<IEnumerable<T>> ParseCore(Cursol cursol)
    {
        var scratch = new ParsedValueScratchBuffer<T>();
        var parseds = new ParsedValueBuffer<T>(scratch);
        try
        {
            var current = cursol;
            while (true)
            {
                var result = Parser.ParseCore(current);
                if (!result.IsSuccess)
                {
                    return ParseResult<IEnumerable<T>>.Success(
                        result.Cursol, parseds.ToArray());
                }

                current = result.Cursol;
                parseds.Add(result.Parsed);
            }
        }
        finally
        {
            parseds.Dispose();
        }
    }
}
