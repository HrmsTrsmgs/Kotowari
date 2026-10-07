using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Benchmarks;

internal enum WordParserAccumulationStrategy
{
    List,
    ListCapacity,
    StringBuilder,
    StringBuilderCapacity,
    IteratorConcat,
    IteratorArray,
    ResultLambda,
    Array,
    DeferredArray,
    StackBuffer
}

internal static class WordParserAccumulationCandidates
{
    internal static WordParserAccumulationParser Create(
        WordParserAccumulationStrategy strategy, string word, bool ignoreCase = false)
        => Create(strategy, word, ignoreCase, new CharParser(' '));

    internal static WordParserAccumulationParser Create(
        WordParserAccumulationStrategy strategy, string word, Parser<char> whiteSpace)
        => Create(strategy, word, false, whiteSpace);

    internal static WordParserAccumulationParser Create(
        WordParserAccumulationStrategy strategy, string word, bool ignoreCase, Parser<char> whiteSpace)
        => strategy switch
        {
            WordParserAccumulationStrategy.List => new ListWordParser(word, ignoreCase, whiteSpace),
            WordParserAccumulationStrategy.ListCapacity => new CapacityListWordParser(word, ignoreCase, whiteSpace),
            WordParserAccumulationStrategy.StringBuilder => new StringBuilderWordParser(word, ignoreCase, whiteSpace),
            WordParserAccumulationStrategy.StringBuilderCapacity => new CapacityStringBuilderWordParser(word, ignoreCase, whiteSpace),
            WordParserAccumulationStrategy.IteratorConcat => new IteratorConcatWordParser(word, ignoreCase, whiteSpace),
            WordParserAccumulationStrategy.IteratorArray => new IteratorArrayWordParser(word, ignoreCase, whiteSpace),
            WordParserAccumulationStrategy.ResultLambda => new ResultLambdaWordParser(word, ignoreCase, whiteSpace),
            WordParserAccumulationStrategy.Array => new ArrayWordParser(word, ignoreCase, whiteSpace),
            WordParserAccumulationStrategy.DeferredArray => new DeferredArrayWordParser(word, ignoreCase, whiteSpace),
            WordParserAccumulationStrategy.StackBuffer => new StackBufferWordParser(word, ignoreCase, whiteSpace),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy))
        };
}

// Only construction and whitespace handling are shared; accumulation is measured independently.
internal abstract class WordParserAccumulationParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : Parser<string>
{
    protected Parser<char>[] Parsers { get; } = [.. word.Select(c => new CharParser(c, ignoreCase))];
    public bool IgnoreCase { get; }
    Parser<char> WhiteSpace { get; } = whiteSpace ?? throw new ArgumentNullException(nameof(whiteSpace));

    protected Cursol<TDocument> SkipBlankAsync<TDocument>(Cursol<TDocument> current)
        where TDocument : IDocument, allows ref struct
    {
        while (true)
        {
            var result = WhiteSpace.Parse(current);
            if (!result.IsSuccess) return current;

            current = result.Cursol;
        }
    }
}

// Baseline body matches HEAD 15dbfed's List accumulation, including the final collection expression.
internal sealed class ListWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);

        var returnValue = new List<char>();
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if (!result.IsSuccess)
            {
                return ParseResult<string, TDocument>.Failure(cursol);
            }

            current = result.Cursol;
            returnValue.Add(result.Parsed);
        }
        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current, new string([.. returnValue]));
    }
}

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

internal sealed class StringBuilderWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);
        var returnValue = new StringBuilder();
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if (!result.IsSuccess) return ParseResult<string, TDocument>.Failure(cursol);

            current = result.Cursol;
            returnValue.Append(result.Parsed);
        }

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current, returnValue.ToString());
    }
}

internal sealed class CapacityStringBuilderWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);
        var returnValue = new StringBuilder(Parsers.Length);
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if (!result.IsSuccess) return ParseResult<string, TDocument>.Failure(cursol);

            current = result.Cursol;
            returnValue.Append(result.Parsed);
        }

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current, returnValue.ToString());
    }
}

internal sealed class IteratorConcatWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
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

        var returnValue = string.Concat(ParseCharacters());
        if (!isSuccess) return ParseResult<string, TDocument>.Failure(cursol);

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current, returnValue);
    }
}

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

internal sealed class ArrayWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);
        var returnValue = new char[Parsers.Length];
        var index = 0;
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if (!result.IsSuccess) return ParseResult<string, TDocument>.Failure(cursol);

            current = result.Cursol;
            returnValue[index++] = result.Parsed;
        }

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current, new string(returnValue));
    }
}

internal sealed class DeferredArrayWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    protected override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);
        char[]? returnValue = null;
        var index = 0;
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if (!result.IsSuccess) return ParseResult<string, TDocument>.Failure(cursol);

            current = result.Cursol;
            returnValue ??= new char[Parsers.Length];
            returnValue[index++] = result.Parsed;
        }

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current, returnValue is null ? string.Empty : new string(returnValue));
    }
}

internal sealed class StackBufferWordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    : WordParserAccumulationParser(word, ignoreCase, whiteSpace)
{
    internal const int StackBufferLimit = 256;

    protected override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);
        Span<char> returnValue = Parsers.Length <= StackBufferLimit
            ? stackalloc char[Parsers.Length]
            : new char[Parsers.Length];
        var index = 0;
        foreach (var parser in Parsers)
        {
            var result = parser.Parse(current);
            if (!result.IsSuccess) return ParseResult<string, TDocument>.Failure(cursol);

            current = result.Cursol;
            returnValue[index++] = result.Parsed;
        }

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current, new string(returnValue));
    }
}
