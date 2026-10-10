using Marimo.Kotowari.Core;

namespace Marimo.Kotowari.Benchmarks;

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
