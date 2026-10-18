using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OneOrMoreParser<T> : Parser<IEnumerable<T>>
    where T : notnull
{
    ZeroOrMoreParser<T> Parser { get; }

    public OneOrMoreParser(Parser<T> parser)
        => Parser = new(parser);

    protected internal override ParseResult<IEnumerable<T>> ParseCore(Cursol cursol)
        => Parser.ParseCore(cursol, requireFirst: true);
}
