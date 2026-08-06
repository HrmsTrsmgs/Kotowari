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

    protected override ParseResult<T> ParseCore(Cursol cursol)
    {
        var result = Parser.Parse(cursol);

        return result.IsSuccess
            ? ParseResult<T>.Success(result.Cursol, Converter(result.Parsed))
            : ParseResult<T>.Failure(result.Cursol);
    }
}
