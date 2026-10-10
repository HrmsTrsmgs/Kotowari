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
    protected internal override ParseResult<(T1, T2), TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<(T1, T2), TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(SequenceParser<T1, T2>)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    sealed class Execution<TDocument> : ParserExecution<(T1, T2), TDocument>
        where TDocument : IDocument, allows ref struct
    {
        readonly ParserExecution<T1, TDocument> parser1;
        readonly ParserExecution<T2, TDocument> parser2;

        Execution(SequenceParser<T1, T2> definition) : base(definition)
        {
            parser1 = definition.Parsers.Item1.CreateExecution<TDocument>();
            parser2 = definition.Parsers.Item2.CreateExecution<TDocument>();
        }

        internal static Execution<TDocument> Get(SequenceParser<T1, T2> definition)
            => ParserExecutionCache<SequenceParser<T1, T2>, Execution<TDocument>>.Get(
                definition, static it => new Execution<TDocument>(it));

        internal override ParseResult<(T1, T2), TDocument> ParseCore(Cursol<TDocument> cursol)
        {
            var result1 = parser1.ParseCore(cursol);
            if (!result1.IsSuccess)
            {
                return ParseResult<(T1, T2), TDocument>.Failure(cursol);
            }

            var result2 = parser2.ParseCore(result1.Cursol);
            if (!result2.IsSuccess)
            {
                return ParseResult<(T1, T2), TDocument>.Failure(cursol);
            }

            return ParseResult<(T1, T2), TDocument>.Success(
                result2.Cursol,
                (result1.Parsed, result2.Parsed));
        }
    }
}
