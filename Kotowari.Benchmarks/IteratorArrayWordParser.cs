using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Benchmarks;


internal sealed class IteratorArrayWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);
        var isSuccess = true;

        IEnumerable<char> ParseCharacters()
        {
            foreach (var parser in Parsers)
            {
                var result = parser.Parse(current);
                if (!result.IsSuccess)
                {
                    isSuccess = false;
                    yield break;
                }

                current = result.Cursol;
                yield return result.Parsed;
            }
        }

        char[] returnValue = [.. ParseCharacters()];
        if (!isSuccess) return ParseResult<string, TDocument>.Failure(cursol);

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current, new string(returnValue));
    }
}
