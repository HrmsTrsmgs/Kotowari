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

    public WordParser(string word, bool ignoreCase = false)
        : this(word, ignoreCase, new CharParser(' '))
    {
    }

    public WordParser(string word, Parser<char> whiteSpace)
        : this(word, false, whiteSpace)
    {
    }

    public WordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    {
        Parsers = word.Select(c => new CharParser(c, ignoreCase));

        WhiteSpace = whiteSpace ?? throw new ArgumentNullException(nameof(whiteSpace));
    }

    protected override ParseResult<string> ParseCore(Cursol cursol)
    {
        var current = SkipBlankAsync(cursol);

        var returnValue = new List<char>();
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if(!result.IsSuccess)
            {
                return ParseResult<string>.Failure(cursol);
            }

            current = result.Cursol;
            returnValue.Add(result.Parsed);
        }
        current = SkipBlankAsync(current);
        return ParseResult<string>.Success(current, new string([.. returnValue]));
    }

    private Cursol SkipBlankAsync(Cursol current)
    {
        while (true)
        {
            var result = WhiteSpace.Parse(current);
            if (!result.IsSuccess) return current;

            current = result.Cursol;
        }
    }
}
