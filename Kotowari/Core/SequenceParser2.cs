using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Transactions;
using System.Data.SqlTypes;

namespace Marimo.Kotowari.Core;

public class SequenceParser<T1, T2> : Parser<(T1, T2)>
{
    (Parser<T1>, Parser<T2>) Parsers { get; }
    public SequenceParser(Parser<T1> parser1, Parser<T2> parser2)
        => Parsers = (parser1, parser2);

    internal override Parser<(T1, T2)> PrepareWhiteSpace<TWhiteSpace>(Parser<TWhiteSpace> whiteSpace)
        => GetType() == typeof(SequenceParser<T1, T2>)
            ? new SequenceParser<T1, T2>(Parsers.Item1.InheritWhiteSpace(whiteSpace), Parsers.Item2)
            : this;

    protected internal override bool TryParseCore(Cursol start, out Cursol next, out (T1, T2) parsed)
    {
        if (!Parsers.Item1.TryParseCore(start, out var next1, out var parsed1))
        {
            next = start;
            parsed = default;
            return false;
        }

        if (!Parsers.Item2.TryParseCore(next1, out var next2, out var parsed2))
        {
            next = start;
            parsed = default;
            return false;
        }

        next = next2;
        parsed = (parsed1, parsed2);
        return true;
    }
}
