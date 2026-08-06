using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Marimo.Kotowari;

public class WithWhiteSpaceParser<T> : Parser<T>
    where T : notnull
{
    Parser<char> WhiteSpace { get; }
    Parser<T> Parser { get; }
    public WithWhiteSpaceParser(Parser<char> whiteSpace, Parser<T> parser)
    {
        WhiteSpace = whiteSpace;
        Parser = parser;
    }

    protected override ParseResult<T> ParseCore(Cursol cursol)
    {
        var current = cursol;
        while (true)
        {
            var result = WhiteSpace.Parse(current);
            if (!result.IsSuccess)
            {
                break;
            }

            current = result.Cursol;
        }

        return Parser.Parse(current);
    }
}
