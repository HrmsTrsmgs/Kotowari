using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Transactions;
using System.Data.SqlTypes;

namespace Marimo.Kotowari.Core;

public class SequenceParser<T1, T2, T3> : Parser<(T1, T2, T3)>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
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
    protected internal override ParseResult<(T1, T2, T3), TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => GetExecution<TDocument>().ParseCore(cursol);

    internal override ParserExecution<(T1, T2, T3), TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(SequenceParser<T1, T2, T3>)
            ? GetExecution<TDocument>()
            : base.CreateExecution<TDocument>();

    ForwardingExecution<(T1, T2, T3), TDocument> GetExecution<TDocument>()
        where TDocument : IDocument, allows ref struct
        => ParserExecutionCache<SequenceParser<T1, T2, T3>, ForwardingExecution<(T1, T2, T3), TDocument>>.Get(
            this, static it => new(it, it.Parser.CreateExecution<TDocument>()));
}
