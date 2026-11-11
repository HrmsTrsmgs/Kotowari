using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Transactions;
using System.Data.SqlTypes;

namespace Marimo.Kotowari.Core;

public class SequenceParser<T1, T2, T3, T4, T5> : Parser<(T1, T2, T3, T4, T5)>
{
    Parser<(T1, T2, T3, T4, T5)> Parser { get; }
    public SequenceParser(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<T4> parser4,
        Parser<T5> parser5)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3, T4, T5)), (T1, T2, T3, T4, T5)>(
            new SequenceParser<T1, (T2, T3, T4, T5)>(
                parser1,
                new SequenceParser<T2, T3, T4, T5>(
                    parser2,
                    parser3,
                    parser4,
                    parser5)),
            tuple => (
                tuple.Item1,
                tuple.Item2.Item1,
                tuple.Item2.Item2,
                tuple.Item2.Item3,
                tuple.Item2.Item4));
    }

    protected internal override bool TryParseCore(Cursol start, out Cursol next, out (T1, T2, T3, T4, T5) parsed)
        => Parser.TryParseCore(start, out next, out parsed);
}
