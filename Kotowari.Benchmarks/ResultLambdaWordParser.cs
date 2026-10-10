using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Benchmarks;


internal sealed class ResultLambdaWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);

        IEnumerable<ParseResult<char, TDocument>> ParseCharacters()
        {
            foreach (var parser in Parsers)
            {
                var result = parser.Parse(current);
                if (!result.IsSuccess)
                {
                    yield return result;
                    yield break;
                }

                current = result.Cursol;
                yield return result;
            }
        }

        Action<Span<char>>? write = null;
        var index = 0;
        foreach (var result in ParseCharacters())
        {
            if (!result.IsSuccess) return ParseResult<string, TDocument>.Failure(cursol);

            var previous = write;
            var character = result.Parsed;
            var characterIndex = index++;
            write = span =>
            {
                previous?.Invoke(span);
                span[characterIndex] = character;
            };
        }

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current,
            string.Create(Parsers.Length, write, static (span, action) => action?.Invoke(span)));
    }
}
