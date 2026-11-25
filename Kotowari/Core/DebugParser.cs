using System;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Core;

/// <summary>解析の直前にコールバックを実行し、子の成否・値・カーソルをそのまま返します。</summary>
/// <param name="parser">実際に入力を解析する子。</param>
/// <param name="hasBreakPoint">子を呼び出す前に実行する処理。</param>
public class DebugParser<T>(Parser<T> parser, Action hasBreakPoint) : Parser<T>
{
    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
    {
        hasBreakPoint();
        return parser.TryParseCore(start, out next, out parsed);
    }
}
