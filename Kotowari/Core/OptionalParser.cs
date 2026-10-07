using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OptionalParser<T> : Parser<Optional<T>>
    where T : notnull
{
    Parser<T> Parser { get; }
    public OptionalParser(Parser<T> parser)
        => Parser = parser;

    protected internal override ParseResult<Optional<T>, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var result = Parser.ParseCore(cursol);
        var parsed = result.IsSuccess ? result.Parsed : default;

        return ParseResult<Optional<T>, TDocument>.Success(
            result.Cursol,
            new Optional<T>(result.IsSuccess, parsed));
    }
}
