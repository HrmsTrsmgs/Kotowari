namespace Marimo.Kotowari.Core;

/// <summary>10 個の子を順に解析し、すべて成功したときに同じ順序のタプルを返します。</summary>
/// <remarks>どれかが失敗したら残りの子は呼ばず、シーケンスの開始位置に戻って失敗します。</remarks>
/// <param name="parser1">開始位置から解析する子。</param>
/// <param name="parser2">第 1 子が成功した位置から解析する子。</param>
/// <param name="parser3">第 2 子が成功した位置から解析する子。</param>
/// <param name="parser4">第 3 子が成功した位置から解析する子。</param>
/// <param name="parser5">第 4 子が成功した位置から解析する子。</param>
/// <param name="parser6">第 5 子が成功した位置から解析する子。</param>
/// <param name="parser7">第 6 子が成功した位置から解析する子。</param>
/// <param name="parser8">第 7 子が成功した位置から解析する子。</param>
/// <param name="parser9">第 8 子が成功した位置から解析する子。</param>
/// <param name="parser10">第 9 子が成功した位置から解析する子。</param>
public class SequenceParser<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
    Parser<T1> parser1,
    Parser<T2> parser2,
    Parser<T3> parser3,
    Parser<T4> parser4,
    Parser<T5> parser5,
    Parser<T6> parser6,
    Parser<T7> parser7,
    Parser<T8> parser8,
    Parser<T9> parser9,
    Parser<T10> parser10)
    : Parser<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>
{
    ParserConverter<(T1, (T2, T3, T4, T5, T6, T7, T8, T9, T10)), (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> Parser { get; } =
        ParserConverter.Create(
            SequenceParser<T1, (T2, T3, T4, T5, T6, T7, T8, T9, T10)>.CreateWithContinuation(
                parser1,
                new SequenceParser<T2, T3, T4, T5, T6, T7, T8, T9, T10>(
                    parser2,
                    parser3,
                    parser4,
                    parser5,
                    parser6,
                    parser7,
                    parser8,
                    parser9,
                    parser10)),
            static values =>
            {
                var (first, (second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth)) = values;
                return (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth);
            });

    internal override Parser<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> PrepareWhiteSpace<TWhiteSpace>(Parser<TWhiteSpace> whiteSpace)
        => GetType() == typeof(SequenceParser<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>)
            ? Parser.PrepareSourceWhiteSpace(whiteSpace)
            : this;

    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) parsed)
        => Parser.TryParseCore(start, out next, out parsed);
}
