using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class RecursiveParser<T>(Func<Parser<T>> parserGetter) : Parser<T>
{
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
        => parserGetter().TryParseCore(start, out next, out parsed);
}
