using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Transactions;
using System.Data.SqlTypes;

namespace Marimo.Kotowari.Core;

public class SequenceParser<T1, T2> : Parser<(T1, T2)>
    where T1 : notnull
    where T2 : notnull
{
    (Parser<T1>, Parser<T2>) Parsers { get; }
    public SequenceParser(Parser<T1> parser1, Parser<T2> parser2)
        => Parsers = (parser1, parser2);
    protected internal override ParseResult<(T1, T2)> ParseCore(Cursol cursol)
    {
        var result1 = Parsers.Item1.ParseCore(cursol);
        if (!result1.IsSuccess)
        {
            return ParseResult<(T1, T2)>.Failure(cursol);
        }

        var result2 = Parsers.Item2.ParseCore(result1.Cursol);
        if (!result2.IsSuccess)
        {
            return ParseResult<(T1, T2)>.Failure(cursol);
        }

        return ParseResult<(T1, T2)>.Success(
            result2.Cursol,
            (result1.Parsed, result2.Parsed));
    }
}
