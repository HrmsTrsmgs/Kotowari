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

    protected override ParseResult<IEnumerable<T>> ParseCore(Cursol cursol)
    {
        var parseds = new List<T>();
        var current = cursol;
        var beforeDelimiter = current;
        while (true)
        {
            var sequenceResult = Sequence.Parse(current);
            if (!sequenceResult.IsSuccess)
            {
                return ParseResult<IEnumerable<T>>.Success(beforeDelimiter, parseds);
            }

            current = sequenceResult.Cursol;
            parseds.Add(sequenceResult.Parsed);
            beforeDelimiter = current;
            var delimiterResult = Delimiter.Parse(current);
            if (!delimiterResult.IsSuccess)
            {
                return ParseResult<IEnumerable<T>>.Success(beforeDelimiter, parseds);
            }

            current = delimiterResult.Cursol;
        }

    }
}
