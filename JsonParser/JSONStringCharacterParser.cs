using Marimo.Kotowari;
using Marimo.Kotowari.Core;

namespace Marimo.Parser;

sealed class JSONStringCharacterParser(Parser<char> parser) : Parser<char>
{
    protected override ParseResult<char> ParseCore(Cursol cursol)
        => cursol.Index < cursol.Text.Length
            ? parser.Parse(cursol)
            : ParseResult<char>.Failure(cursol);
}
