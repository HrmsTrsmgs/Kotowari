using Marimo.Kotowari.Core;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Tests.Core;

internal sealed class NullStringParser : Parser<string?>
{
    protected override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out string? parsed)
    {
        next = start.GoFoward(1);
        parsed = null;
        return start.Current == 'a';
    }
}
