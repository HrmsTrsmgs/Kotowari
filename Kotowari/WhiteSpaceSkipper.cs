using Marimo.Kotowari.Core;

namespace Marimo.Kotowari;

internal static class WhiteSpaceSkipper
{
    public static Cursol Skip<T>(Parser<T> whiteSpace, Cursol cursol)
        where T : notnull
    {
        var current = cursol;
        while (true)
        {
            var result = whiteSpace.Parse(current);
            if (!result.IsSuccess)
            {
                return current;
            }

            current = result.Cursol;
        }
    }
}
