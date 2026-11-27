using Marimo.Kotowari.Core;
using System;

namespace Marimo.Kotowari;

/// <summary>前置・後置に共通する空白の反復処理です。</summary>
internal static class WhiteSpaceSkipper
{
    /// <summary>空白が入力を消費して失敗した場合も、その失敗で進んだ位置は返しません。</summary>
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
