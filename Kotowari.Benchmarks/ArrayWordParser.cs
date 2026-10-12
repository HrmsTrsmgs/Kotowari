using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Benchmarks;

internal sealed class ArrayWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string> ParseCore(Cursol cursol)
    {
        var current = SkipBlankAsync(cursol);
        var returnValue = new char[Parsers.Length];
        var index = 0;
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if (!result.IsSuccess) return ParseResult<string>.Failure(cursol);

            current = result.Cursol;
            returnValue[index++] = result.Parsed;
        }

        current = SkipBlankAsync(current);
        return ParseResult<string>.Success(current, new string(returnValue));
    }
}
