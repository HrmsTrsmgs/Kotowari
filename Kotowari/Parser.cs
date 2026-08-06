using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari;

public abstract class Parser
{
}

public abstract class Parser<T> : Parser
    where T : notnull
{
    public ParseResult<T> Parse(Cursol cursol)
        => ParseCore(cursol);

    protected abstract ParseResult<T> ParseCore(Cursol cursol);
}
