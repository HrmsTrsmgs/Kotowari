using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class DebugParser<T>(Parser<T> parser, Action hasBreakPoint) : Parser<T>
    where T : notnull
{
    protected override ParseResult<T> ParseCore(Cursol cursol)
    {
        hasBreakPoint();
        return parser.Parse(cursol);
    }
}
