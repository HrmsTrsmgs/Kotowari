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

    protected override ParseResult<IEnumerable<T>> ParseCore(Cursol cursol)
        => Parser.Parse(cursol);
}
