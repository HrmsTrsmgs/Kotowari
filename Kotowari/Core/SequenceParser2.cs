namespace Marimo.Kotowari.Core;

/// <summary>二つの子を順に解析し、両方が成功したときだけそれぞれの値を返します。</summary>
/// <remarks>どちらかが失敗したら残りの子は呼ばず、シーケンスの開始位置に戻って失敗します。</remarks>
/// <param name="parser1">開始位置から解析する子。</param>
/// <param name="parser2">最初の子が成功した位置から解析する子。</param>
public class SequenceParser<T1, T2>(Parser<T1> parser1, Parser<T2> parser2) : Parser<(T1, T2)>
{
    (Parser<T1>, Parser<T2>) Parsers { get; } = (parser1, parser2);

    internal static Parser<(T1, T2)> CreatePrepared<TWhiteSpace>(
        Parser<T1> first,
        Parser<T2> last,
        Parser<TWhiteSpace> whiteSpace)
        => new SequenceParser<T1, T2>(first.InheritWhiteSpace(whiteSpace), last);
    internal override Parser<(T1, T2)> PrepareWhiteSpace<TWhiteSpace>(Parser<TWhiteSpace> whiteSpace)
        => GetType() == typeof(SequenceParser<T1, T2>)
            ? CreatePrepared(Parsers.Item1, Parsers.Item2, whiteSpace)
            : this;

    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out (T1, T2) parsed)
    {
        if (Parsers.Item1.TryParseCore(start, out var next1, out var parsed1)
            && Parsers.Item2.TryParseCore(next1, out var next2, out var parsed2))
        {
            next = next2;
            parsed = (parsed1, parsed2);
            return true;
        }

        next = start;
        parsed = default;
        return false;
    }
}
