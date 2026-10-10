using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OneOrMoreParser<T> : Parser<IEnumerable<T>>
    where T : notnull
{
    Parser<IEnumerable<T>> Parser { get; }

    public OneOrMoreParser(Parser<T> parser)
    {
        Parser =
            new ParserConverter<(T, IEnumerable<T>), IEnumerable<T>>(
                new SequenceParser<T, IEnumerable<T>>(
                    parser,
                    new ZeroOrMoreParser<T>(parser)),
                tuple => new[] { tuple.Item1 }.Concat(tuple.Item2));
    }

    protected internal override ParseResult<IEnumerable<T>, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => GetExecution<TDocument>().ParseCore(cursol);

    internal override ParserExecution<IEnumerable<T>, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(OneOrMoreParser<T>)
            ? GetExecution<TDocument>()
            : base.CreateExecution<TDocument>();

    ForwardingExecution<IEnumerable<T>, TDocument> GetExecution<TDocument>()
        where TDocument : IDocument, allows ref struct
        => ParserExecutionCache<OneOrMoreParser<T>, ForwardingExecution<IEnumerable<T>, TDocument>>.Get(
            this, static it => new(it, it.Parser.CreateExecution<TDocument>()));
}
