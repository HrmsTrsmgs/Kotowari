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
    (Parser<T1>, Parser<T2>, Parser<T3>) Parsers { get; } = (parser1, parser2, parser3);

    Parser<(T1, T2, T3)> Parser { get; } =
        CreateParser(
            parser1,
            new SequenceParser<T2, T3>(
                parser2,
                parser3));

    static Parser<(T1, T2, T3)> CreateParser(Parser<T1> first, Parser<(T2, T3)> remaining)
        => new ParserConverter<(T1, (T2, T3)), (T1, T2, T3)>(
            new SequenceParser<T1, (T2, T3)>(first, remaining),
            tuple => (tuple.Item1, tuple.Item2.Item1, tuple.Item2.Item2));

    internal static Parser<(T1, T2, T3)> CreatePrepared<TWhiteSpace>(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<TWhiteSpace> whiteSpace)
        => CreateParser(
            parser1.InheritWhiteSpace(whiteSpace),
            SequenceParser<T2, T3>.CreatePrepared(
                parser2,
                parser3,
                whiteSpace));

    internal override Parser<(T1, T2, T3)> PrepareWhiteSpace<TWhiteSpace>(Parser<TWhiteSpace> whiteSpace)
        => GetType() == typeof(SequenceParser<T1, T2, T3>)
            ? CreatePrepared(
                Parsers.Item1,
                Parsers.Item2,
                Parsers.Item3,
                whiteSpace)
            : this;
    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out (T1, T2, T3) parsed)
        => Parser.TryParseCore(start, out next, out parsed);
}
