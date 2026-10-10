using BenchmarkDotNet.Attributes;
using Marimo.Kotowari.Core;
using System;
using System.Linq;

namespace Marimo.Kotowari.Benchmarks;


public enum WordParserAccumulationCase
{
    False5Success,
    False5FirstFailure,
    False5LastFailure,
    True4Success,
    True4FirstFailure,
    True4LastFailure,
    Long32Success,
    Long32FirstFailure,
    Long32LastFailure,
    Long128Success,
    Long128FirstFailure,
    Long128LastFailure
}
