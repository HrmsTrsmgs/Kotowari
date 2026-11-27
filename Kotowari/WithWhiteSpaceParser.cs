using Marimo.Kotowari.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari;

/// <summary>前置空白を読み飛ばしてから子を解析します。後置空白を指定する WithWhiteSpace とは適用位置が異なります。</summary>
/// <param name="whiteSpace">繰り返し読み飛ばす空白。成功するたびに入力位置を変える必要があります。</param>
/// <param name="parser">空白の直後から解析する子。</param>
public class WithWhiteSpaceParser<T>(Parser<char> whiteSpace, Parser<T> parser) : Parser<T>
{
    Parser<char> WhiteSpace { get; } = whiteSpace;
    Parser<T> Parser { get; } = parser;

    /// <summary>前置空白を読み飛ばして値を解析します。</summary>
    /// <exception cref="InvalidOperationException">空白パーサーが入力位置を進めずに成功した場合にスローされます。</exception>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
        => Parser.TryParseCore(WhiteSpaceSkipper.Skip(WhiteSpace, start), out next, out parsed);
}
