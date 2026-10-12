using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Benchmarks;

internal sealed class StringBuilderWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string> ParseCore(Cursol cursol)
    {
        var current = SkipBlankAsync(cursol);
        var returnValue = new StringBuilder();
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if (!result.IsSuccess) return ParseResult<string>.Failure(cursol);

            current = result.Cursol;
            returnValue.Append(result.Parsed);
        }

        current = SkipBlankAsync(current);
        return ParseResult<string>.Success(current, returnValue.ToString());
    }
}
