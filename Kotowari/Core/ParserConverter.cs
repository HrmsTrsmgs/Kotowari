using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ParserConverter<U, T> : Parser<T>
    where U : notnull
    where T : notnull
{
    Parser<U> Parser { get; }
    Func<U, T> Converter { get; }

    public ParserConverter(Parser<U> parser, Func<U, T> converter)
    {
        Parser = parser;
        Converter = converter;
    }

    protected internal override ParseResult<T, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var result = Parser.ParseCore(cursol);

        return result.IsSuccess
            ? ParseResult<T, TDocument>.Success(result.Cursol, Converter(result.Parsed))
            : ParseResult<T, TDocument>.Failure(result.Cursol);
    }
}
