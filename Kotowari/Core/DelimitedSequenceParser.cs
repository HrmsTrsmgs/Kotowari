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
    {
        var parseds = new List<T>();
        var current = cursol;
        var beforeDelimiter = current;
        while (true)
        {
            var sequenceResult = Sequence.ParseCore(current);
            if (!sequenceResult.IsSuccess)
            {
                return ParseResult<IEnumerable<T>, TDocument>.Success(beforeDelimiter, parseds);
            }

            current = sequenceResult.Cursol;
            parseds.Add(sequenceResult.Parsed);
            beforeDelimiter = current;
            var delimiterResult = Delimiter.ParseCore(current);
            if (!delimiterResult.IsSuccess)
            {
                return ParseResult<IEnumerable<T>, TDocument>.Success(beforeDelimiter, parseds);
            }

            current = delimiterResult.Cursol;
        }

    }
}
