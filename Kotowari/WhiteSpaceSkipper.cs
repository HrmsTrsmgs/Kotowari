using Marimo.Kotowari.Core;
using System;

namespace Marimo.Kotowari;

internal static class WhiteSpaceSkipper
{
    public static Cursol Skip<T>(Parser<T> whiteSpace, Cursol cursol)
    {
        var current = cursol;
        while (true)
        {
            if (!whiteSpace.TryParseCore(current, out var next, out _))
            {
                return current;
            }

            if (next.Index == current.Index)
            {
                throw new InvalidOperationException("空白パーサーが入力を消費せず成功しました。");
            }

            current = next;
        }
    }
}
