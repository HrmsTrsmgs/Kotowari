namespace Marimo.Kotowari.Core;

/// <summary>3 個の子を順に解析し、すべて成功したときに同じ順序のタプルを返します。</summary>
/// <remarks>どれかが失敗したら残りの子は呼ばず、シーケンスの開始位置に戻って失敗します。</remarks>
/// <param name="parser1">開始位置から解析する子。</param>
/// <param name="parser2">第 1 子が成功した位置から解析する子。</param>
/// <param name="parser3">第 2 子が成功した位置から解析する子。</param>
public class SequenceParser<T1, T2, T3>(
    Parser<T1> parser1,
    Parser<T2> parser2,
    Parser<T3> parser3)
    : Parser<(T1, T2, T3)>
{
    ParserConverter<(T1, (T2, T3)), (T1, T2, T3)> Parser { get; } =
        ParserConverter.Create(
            SequenceParser<T1, (T2, T3)>.CreateWithContinuation(
                parser1,
                SequenceParser.Create(parser2, parser3)),
            static values =>
            {
                var (first, (second, third)) = values;
                return (first, second, third);
            });

    internal override Parser<(T1, T2, T3)> PrepareWhiteSpace<TWhiteSpace>(Parser<TWhiteSpace> whiteSpace)
        => GetType() == typeof(SequenceParser<T1, T2, T3>)
            ? Parser.PrepareSourceWhiteSpace(whiteSpace)
            : this;

    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out (T1, T2, T3) parsed)
        => Parser.TryParseCore(start, out next, out parsed);
}
