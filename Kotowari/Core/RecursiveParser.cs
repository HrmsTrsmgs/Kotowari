using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class RecursiveParser<T>(Func<Parser<T>> parserGetter) : Parser<T>
    where T : notnull
{
    protected internal override ParseResult<T, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => parserGetter().ParseCore(cursol);
}
