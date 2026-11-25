using Marimo.Kotowari.Core;

namespace Marimo.Kotowari;

/// <summary>子パーサーの結果型から省略可能なパーサーを構築します。</summary>
public static class OptionalParser
{
    /// <summary>子の失敗を入力消費のない不在値として扱います。</summary>
    /// <param name="parser">値の有無を判定する子パーサー。</param>
    /// <returns>子の成功値または不在値を返すパーサー。</returns>
    public static OptionalParser<T> Create<T>(Parser<T> parser)
        => new(parser);
}
