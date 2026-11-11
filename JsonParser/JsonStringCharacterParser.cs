using Marimo.Kotowari;
using Marimo.Kotowari.Core;

namespace Marimo.Parser;

sealed class JsonStringCharacterParser(Parser<char> parser) : Parser<char>
{
    protected override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        if (start.Index < start.Text.Length)
        {
            return TryParseChild(parser, start, out next, out parsed);
        }

        next = start;
        parsed = default;
        return false;
    }
}
