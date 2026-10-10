using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Marimo.Kotowari.Core;

public class DelimitedSequenceParser<T, U> : Parser<IEnumerable<T>>
    where T : notnull
    where U : notnull
{
    Parser<T> Sequence { get; }
    Parser<U> Delimiter { get; }
    public DelimitedSequenceParser(Parser<T> sequence, Parser<U> delimiter)
    {
        Sequence = sequence;
        Delimiter = delimiter;
    }

    protected internal override ParseResult<IEnumerable<T>, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<IEnumerable<T>, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(DelimitedSequenceParser<T, U>)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    sealed class Execution<TDocument> : ParserExecution<IEnumerable<T>, TDocument>
        where TDocument : IDocument, allows ref struct
    {
        readonly ParserExecution<T, TDocument> sequence;
        readonly ParserExecution<U, TDocument> delimiter;

        Execution(DelimitedSequenceParser<T, U> definition) : base(definition)
        {
            sequence = definition.Sequence.CreateExecution<TDocument>();
            delimiter = definition.Delimiter.CreateExecution<TDocument>();
        }

        internal static Execution<TDocument> Get(DelimitedSequenceParser<T, U> definition)
            => ParserExecutionCache<DelimitedSequenceParser<T, U>, Execution<TDocument>>.Get(
                definition, static it => new Execution<TDocument>(it));

        internal override ParseResult<IEnumerable<T>, TDocument> ParseCore(Cursol<TDocument> cursol)
        {
            var parseds = new List<T>();
            var current = cursol;
            var beforeDelimiter = current;
            while (true)
            {
                var sequenceResult = sequence.ParseCore(current);
                if (!sequenceResult.IsSuccess)
                {
                    return ParseResult<IEnumerable<T>, TDocument>.Success(beforeDelimiter, parseds);
                }

                current = sequenceResult.Cursol;
                parseds.Add(sequenceResult.Parsed);
                beforeDelimiter = current;
                var delimiterResult = delimiter.ParseCore(current);
                if (!delimiterResult.IsSuccess)
                {
                    return ParseResult<IEnumerable<T>, TDocument>.Success(beforeDelimiter, parseds);
                }

                current = delimiterResult.Cursol;
            }
        }
    }
}
