using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OrParser<T> : Parser<T>
    where T : notnull
{
    Parser<T>[] Parsers { get; }

    public OrParser(params Parser<T>[] parsers)
        => Parsers = parsers;

    protected override ParseResult<T> ParseCore(Cursol cursol)
    {
        foreach(var parser in Parsers)
        {
            var result = parser.Parse(cursol);

            if(result.IsSuccess)
            {
                return result;
            }
        }
        return ParseResult<T>.Failure(cursol);
    }
}
