using System;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Core;

/// <summary>子の成功値だけを変換し、子の成否と解析後のカーソルを保ちます。</summary>
/// <param name="parser">変換前の値を返すパーサー。</param>
/// <param name="converter">成功値を変換する関数。子が失敗した場合は呼ばれません。</param>
public class ParserConverter<U, T>(Parser<U> parser, Func<U, T> converter) : Parser<T>
{
    Parser<U> Parser { get; } = parser;
    Func<U, T> Converter { get; } = converter;

    // 投影はそのままに、Sequenceの内部構造に空白規則を渡す。
    internal ParserConverter<U, T> PrepareSourceWhiteSpace<TWhiteSpace>(Parser<TWhiteSpace> whiteSpace)
        => new(Parser.PrepareWhiteSpace(whiteSpace), Converter);

    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
    {
        if (!Parser.TryParseCore(start, out next, out var childParsed))
        {
            parsed = default;
            return false;
        }

        parsed = Converter(childParsed);
        return true;
    }
}
