using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Marimo.Kotowari.Core
{
    public class DelimitedSequenceParser<T, U> : Parser<IEnumerable<T>>
    {
        Parser<T> Sequence { get; }
        Parser<U> Delimiter { get; }
        public DelimitedSequenceParser(Parser<T> sequence, Parser<U> delimiter)
        {
            Sequence = sequence;
            Delimiter = delimiter;
        }

        protected override (bool isSuccess, Cursol cursol, IEnumerable<T> parsed) ParseCore(Cursol cursol)
        {
            var parseds = new List<T>();
            var current = cursol;
            bool isSuccess;
            T parsed;
            var beforeDelimiter = current;
            while (true)
            {
                (isSuccess, current, parsed) = Sequence.Parse(current);
                if (!isSuccess)
                {
                    return (true, beforeDelimiter, parseds);
                }
                parseds.Add(parsed);
                beforeDelimiter = current;
                (isSuccess, current, _) = Delimiter.Parse(current);
                if (!isSuccess)
                {
                    return (true, beforeDelimiter, parseds);
                }
            }
            
        }
    }
}
