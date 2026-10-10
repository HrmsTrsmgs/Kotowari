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
