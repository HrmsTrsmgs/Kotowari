using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class RecursiveParser<T> : Parser<T>
    where T : notnull
{
    Func<Parser<T>> ParserGetter { get; }
    public RecursiveParser(Func<Parser<T>> parserGetter)
        => ParserGetter = parserGetter;

    protected override ParseResult<T> ParseCore(Cursol cursol)
        => ParserGetter().Parse(cursol);
}
