using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari;

public static class SequenceParser
{
    public static SequenceParser<T1, T2> Create<T1, T2>(Parser<T1> parser1, Parser<T2> parser2)
        where T1 : notnull
        where T2 : notnull
        => new(parser1, parser2);

    public static SequenceParser<T1, T2, T3> Create<T1, T2, T3>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        => new(parser1, parser2, parser3);
}
