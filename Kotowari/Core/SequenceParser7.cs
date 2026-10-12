using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Transactions;
using System.Data.SqlTypes;

namespace Marimo.Kotowari.Core;

public class SequenceParser<T1, T2, T3, T4, T5, T6, T7> : Parser<(T1, T2, T3, T4, T5, T6, T7)>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull
    where T7 : notnull
{
    Parser<(T1, T2, T3, T4, T5, T6, T7)> Parser { get; }
    public SequenceParser(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<T4> parser4,
        Parser<T5> parser5,
        Parser<T6> parser6,
        Parser<T7> parser7)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3, T4, T5, T6, T7)), (T1, T2, T3, T4, T5, T6, T7)>(
            new SequenceParser<T1, (T2, T3, T4, T5, T6, T7)>(
                parser1,
                new SequenceParser<T2, T3, T4, T5, T6, T7>(
                    parser2,
                    parser3,
                    parser4,
                    parser5,
                    parser6,
                    parser7)),
            tuple => (
                tuple.Item1,
                tuple.Item2.Item1,
                tuple.Item2.Item2,
                tuple.Item2.Item3,
                tuple.Item2.Item4,
                tuple.Item2.Item5,
                tuple.Item2.Item6));
    }

    protected internal override ParseResult<(T1, T2, T3, T4, T5, T6, T7)> ParseCore(Cursol cursol)
        => Parser.ParseCore(cursol);
}
