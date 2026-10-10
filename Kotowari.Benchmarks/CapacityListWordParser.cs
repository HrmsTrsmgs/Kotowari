using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Benchmarks;


internal sealed class CapacityListWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);
        var returnValue = new List<char>(Parsers.Length);
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if (!result.IsSuccess) return ParseResult<string, TDocument>.Failure(cursol);

            current = result.Cursol;
            returnValue.Add(result.Parsed);
        }

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current, new string([.. returnValue]));
    }
}
