using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Transactions;
using System.Data.SqlTypes;

namespace Marimo.Kotowari.Core;

public class SequenceParser<T1, T2, T3> : Parser<(T1, T2, T3)>
{
    Parser<(T1, T2, T3)> Parser { get; }
    public SequenceParser(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3)), (T1, T2, T3)>(
            new SequenceParser<T1, (T2, T3)>(
                parser1,
                new SequenceParser<T2, T3>(
                    parser2,
                    parser3)),
            tuple => (tuple.Item1, tuple.Item2.Item1, tuple.Item2.Item2));
    }
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out (T1, T2, T3) parsed)
        => Parser.TryParseCore(start, out next, out parsed);
}
