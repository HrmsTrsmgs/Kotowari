using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OptionalParser<T> : Parser<Optional<T>>
{
    Parser<T> Parser { get; }
    public OptionalParser(Parser<T> parser)
        => Parser = parser;

    protected override (bool isSuccess, Cursol cursol, Optional<T> parsed) ParseCore(Cursol cursol)
    {
        var (isSuccess, afterCursol, parsed) = Parser.Parse(cursol);
        return (true, afterCursol, new Optional<T>(isSuccess, parsed));
    }
}
