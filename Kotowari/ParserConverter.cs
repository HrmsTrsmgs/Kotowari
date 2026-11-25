using Marimo.Kotowari.Core;
using System;

namespace Marimo.Kotowari;

/// <summary>子パーサーと変換関数から結果型を推論して変換パーサーを構築します。</summary>
public static class ParserConverter
{
    /// <summary>子の成否とカーソルを引き継ぎ、成功値を変換します。</summary>
    /// <param name="parser">変換する値を解析する子パーサー。</param>
    /// <param name="converter">子が成功したときだけその値に適用する関数。</param>
    /// <returns>変換後の値を返すパーサー。</returns>
    public static ParserConverter<U, T> Create<T, U>(Parser<U> parser, Func<U, T> converter)
        => new(parser, converter);
}
