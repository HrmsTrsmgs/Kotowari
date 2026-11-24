using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class DebugParser<T>(Parser<T> parser, Action hasBreakPoint) : Parser<T>
{
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
    {
        hasBreakPoint();
        return parser.TryParseCore(start, out next, out parsed);
    }
}
