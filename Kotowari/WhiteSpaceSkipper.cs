using Marimo.Kotowari.Core;

namespace Marimo.Kotowari;

internal static class WhiteSpaceSkipper
{
    public static Cursol Skip<T>(Parser<T> whiteSpace, Cursol cursol)
    {
        var current = cursol;
        while (true)
        {
            if (!whiteSpace.TryParseCore(current, out var next, out _))
            {
                return current;
            }

            current = next;
        }
    }
}
