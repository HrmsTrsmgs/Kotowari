using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class DebugParser<T>(Parser<T> parser, Action hasBreakPoint) : Parser<T>
    where T : notnull
{
    protected internal override ParseResult<T, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        hasBreakPoint();
        return parser.ParseCore(cursol);
    }
}
