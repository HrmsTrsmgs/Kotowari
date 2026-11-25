namespace Marimo.Kotowari.Benchmarks;

internal enum WordParserAccumulationStrategy
{
    List,
    ListCapacity,
    StringBuilder,
    StringBuilderCapacity,
    Array,
    DeferredArray,
    StackBuffer
}
