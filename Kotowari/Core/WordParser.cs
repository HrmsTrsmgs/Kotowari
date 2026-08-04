using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http.Headers;
using System.Transactions;

namespace Marimo.Kotowari.Core;

public class WordParser : Parser<string>
{
    IEnumerable<Parser<char>> Parsers { get; }
    public bool IgnoreCase { get; }

    Parser<char> WhiteSpace { get; }

    public WordParser(string word, bool ignoreCase = false, Parser<char> whiteSpace = null)
    {
        Parsers = word.Select(c => new CharParser(c, ignoreCase));

        WhiteSpace = whiteSpace ?? new CharParser(' ');
    }

    protected override (bool isSuccess,Cursol cursol, string parsed)  ParseCore(Cursol cursol)
    {
        var current = SkipBlankAsync(cursol);

        var returnValue = new List<char>();
        foreach (var parser in Parsers)
        {
            bool isSuccess;
            char parsed;
            (isSuccess, current, parsed) = parser.Parse(current);
            if(!isSuccess)
            {
                return (false, cursol, null);
            }
            returnValue.Add(parsed);
        }
        current = SkipBlankAsync(current);
        return (true, current, new string([.. returnValue]));
    }

    private Cursol SkipBlankAsync(Cursol current)
    {
        while (true)
        {
            bool isSuccess;
            (isSuccess, current, _) = WhiteSpace.Parse(current);
            if (!isSuccess) return current;
        }
    }
}
