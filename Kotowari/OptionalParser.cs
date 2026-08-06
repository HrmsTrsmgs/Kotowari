using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari;

public static class OptionalParser
{
    public static OptionalParser<T> Create<T>(Parser<T> parser)
        where T : notnull
        => new OptionalParser<T>(parser);
}
