using Marimo.Kotowari.Core;

namespace Marimo.Kotowari;

/// <summary>子の結果型を推論して順序付きのパーサーを構築します。</summary>
public static class SequenceParser
{
    /// <summary>二つの子を順に試し、すべての成功値を同じ順序のタプルで返すパーサーを作ります。</summary>
    /// <param name="parser1">開始位置から解析する子。</param>
    /// <param name="parser2">最初の子が成功した位置から解析する子。</param>
    /// <returns>どちらかの子が失敗すれば開始位置に戻るシーケンス。</returns>
    public static SequenceParser<T1, T2> Create<T1, T2>(Parser<T1> parser1, Parser<T2> parser2)
        => new(parser1, parser2);

    /// <summary>三つの子を順に試し、すべての成功値を同じ順序のタプルで返すパーサーを作ります。</summary>
    /// <param name="parser1">開始位置から解析する子。</param>
    /// <param name="parser2">最初の子が成功した位置から解析する子。</param>
    /// <param name="parser3">二番目の子が成功した位置から解析する子。</param>
    /// <returns>いずれかの子が失敗すれば開始位置に戻るシーケンス。</returns>
    public static SequenceParser<T1, T2, T3> Create<T1, T2, T3>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3)
        => new(parser1, parser2, parser3);
}
