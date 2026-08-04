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
{
    public (bool isSuccess, Cursol cursol, T parsed) Parse(Cursol cursol)
    {
        return ParseCore(cursol);
    }

    protected abstract (bool isSuccess, Cursol cursol, T parsed) ParseCore(Cursol cursol);
}
