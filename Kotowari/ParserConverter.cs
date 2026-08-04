using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari;

public static class ParserConverter
{
    public static ParserConverter<U, T> Create<T, U>(Parser<U> parser, Func<U, T> converter)
    {
        return new ParserConverter<U, T>(parser, converter);
    }
}
