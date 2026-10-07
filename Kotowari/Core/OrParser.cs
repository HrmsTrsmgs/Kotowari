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

    protected internal override ParseResult<T, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        foreach(var parser in Parsers)
        {
            var result = parser.ParseCore(cursol);

            if(result.IsSuccess)
            {
                return result;
            }
        }
        return ParseResult<T, TDocument>.Failure(cursol);
    }
}
