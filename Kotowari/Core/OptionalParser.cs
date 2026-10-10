using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OptionalParser<T> : Parser<Optional<T>>
    where T : notnull
{
    Parser<T> Parser { get; }
    public OptionalParser(Parser<T> parser)
        => Parser = parser;

    protected internal override ParseResult<Optional<T>, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<Optional<T>, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(OptionalParser<T>)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    sealed class Execution<TDocument> : ParserExecution<Optional<T>, TDocument>
        where TDocument : IDocument, allows ref struct
    {
        readonly ParserExecution<T, TDocument> parser;

        Execution(OptionalParser<T> definition) : base(definition)
            => parser = definition.Parser.CreateExecution<TDocument>();

        internal static Execution<TDocument> Get(OptionalParser<T> definition)
            => ParserExecutionCache<OptionalParser<T>, Execution<TDocument>>.Get(
                definition, static it => new Execution<TDocument>(it));

        internal override ParseResult<Optional<T>, TDocument> ParseCore(Cursol<TDocument> cursol)
        {
            var result = parser.ParseCore(cursol);
            var parsed = result.IsSuccess ? result.Parsed : default;

            return ParseResult<Optional<T>, TDocument>.Success(
                result.Cursol,
                new Optional<T>(result.IsSuccess, parsed));
        }
    }
}
