using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class RecursiveParser<T>(Func<Parser<T>> parserGetter) : Parser<T>
    where T : notnull
{
    protected override ParseResult<T> ParseCore(Cursol cursol)
        => parserGetter().Parse(cursol);
}
