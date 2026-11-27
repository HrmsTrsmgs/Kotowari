namespace Marimo.Kotowari.Core;

/// <summary>6 個の子を順に解析し、すべて成功したときに同じ順序のタプルを返します。</summary>
/// <remarks>どれかが失敗したら残りの子は呼ばず、シーケンスの開始位置に戻って失敗します。</remarks>
/// <param name="parser1">開始位置から解析する子。</param>
/// <param name="parser2">第 1 子が成功した位置から解析する子。</param>
/// <param name="parser3">第 2 子が成功した位置から解析する子。</param>
/// <param name="parser4">第 3 子が成功した位置から解析する子。</param>
/// <param name="parser5">第 4 子が成功した位置から解析する子。</param>
/// <param name="parser6">第 5 子が成功した位置から解析する子。</param>
public class SequenceParser<T1, T2, T3, T4, T5, T6>(
    Parser<T1> parser1,
    Parser<T2> parser2,
    Parser<T3> parser3,
    Parser<T4> parser4,
    Parser<T5> parser5,
    Parser<T6> parser6)
    : Parser<(T1, T2, T3, T4, T5, T6)>
{
    (Parser<T1>, Parser<T2>, Parser<T3>, Parser<T4>, Parser<T5>, Parser<T6>) Parsers { get; } = (parser1, parser2, parser3, parser4, parser5, parser6);

    Parser<(T1, T2, T3, T4, T5, T6)> Parser { get; } =
        new ParserConverter<(T1, (T2, T3, T4, T5, T6)), (T1, T2, T3, T4, T5, T6)>(
            new SequenceParser<T1, (T2, T3, T4, T5, T6)>(
                parser1,
                new SequenceParser<T2, T3, T4, T5, T6>(
                    parser2,
                    parser3,
                    parser4,
                    parser5,
                    parser6)),
            tuple => (
                tuple.Item1,
                tuple.Item2.Item1,
                tuple.Item2.Item2,
                tuple.Item2.Item3,
                tuple.Item2.Item4,
                tuple.Item2.Item5));

    internal override Parser<(T1, T2, T3, T4, T5, T6)> PrepareWhiteSpace<TWhiteSpace>(Parser<TWhiteSpace> whiteSpace)
        => GetType() == typeof(SequenceParser<T1, T2, T3, T4, T5, T6>)
            ? new SequenceParser<T1, T2, T3, T4, T5, T6>(
                Parsers.Item1.InheritWhiteSpace(whiteSpace),
                Parsers.Item2.InheritWhiteSpace(whiteSpace),
                Parsers.Item3.InheritWhiteSpace(whiteSpace),
                Parsers.Item4.InheritWhiteSpace(whiteSpace),
                Parsers.Item5.InheritWhiteSpace(whiteSpace),
                Parsers.Item6)
            : this;

    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out (T1, T2, T3, T4, T5, T6) parsed)
        => Parser.TryParseCore(start, out next, out parsed);
}
