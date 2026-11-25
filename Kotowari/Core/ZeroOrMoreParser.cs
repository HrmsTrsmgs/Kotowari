using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Core;

/// <summary>子が失敗するまで成功値を順に集めます。最初から失敗しても空の列で成功し、停止した子のカーソルを返します。</summary>
/// <param name="parser">反復するパーサー。成功した場合は入力位置を変える必要があります。</param>
public class ZeroOrMoreParser<T>(Parser<T> parser) : Parser<IEnumerable<T>>
{
    Parser<T> Parser { get; } = parser;

    /// <summary>要素を繰り返し解析します。</summary>
    /// <exception cref="InvalidOperationException">要素パーサーが入力位置を進めずに成功した場合にスローされます。</exception>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out IEnumerable<T> parsed)
        => TryParseCore(start, out next, out parsed, requireFirst: false);

    // OneOrMoreも先頭から同じバッファに集め、一要素配列とConcat iteratorを作らない。
    // 最初の失敗だけは開始位置へ戻し、後続の失敗では子が返した位置を引き継ぐ。
    // 測定・採否: Kotowari.Benchmarks/Results/2026-10-19-one-or-more-buffer.md。
    internal bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out IEnumerable<T> parsed, bool requireFirst)
    {
        var scratch = new ParsedValueScratchBuffer<T>();
        var parseds = new ParsedValueBuffer<T>(scratch);
        try
        {
            var current = start;
            while (true)
            {
                if (!Parser.TryParseCore(current, out var childNext, out var childParsed))
                {
                    if (requireFirst && parseds.Count == 0)
                    {
                        next = start;
                        parsed = default;
                        return false;
                    }

                    next = childNext;
                    parsed = parseds.ToArray();
                    return true;
                }

                if (childNext.Index == current.Index)
                {
                    throw new InvalidOperationException("反復する要素パーサーが入力を消費せず成功しました。");
                }

                current = childNext;
                parseds.Add(childParsed);
            }
        }
        finally
        {
            parseds.Dispose();
        }
    }
}
