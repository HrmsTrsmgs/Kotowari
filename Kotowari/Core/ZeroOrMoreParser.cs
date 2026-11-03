using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ZeroOrMoreParser<T> : Parser<IEnumerable<T>>
    where T : notnull
{
    Parser<T> Parser { get; }

    public ZeroOrMoreParser(Parser<T> parser)
        => Parser = parser;

    /// <summary>要素を繰り返し解析します。</summary>
    /// <exception cref="InvalidOperationException">要素パーサーが入力位置を進めずに成功した場合にスローされます。</exception>
    protected internal override ParseResult<IEnumerable<T>> ParseCore(Cursol cursol)
        => ParseCore(cursol, requireFirst: false);

    // OneOrMoreも先頭から同じバッファに集め、一要素配列とConcat iteratorを作らない。
    // 最初の失敗だけは開始位置へ戻し、後続の失敗では子が返した位置を引き継ぐ。
    // 測定・採否: Kotowari.Benchmarks/Results/2026-10-19-one-or-more-buffer.md。
    internal ParseResult<IEnumerable<T>> ParseCore(Cursol cursol, bool requireFirst)
    {
        var scratch = new ParsedValueScratchBuffer<T>();
        var parseds = new ParsedValueBuffer<T>(scratch);
        try
        {
            var current = cursol;
            while (true)
            {
                var result = Parser.ParseCore(current);
                if (!result.IsSuccess)
                {
                    if (requireFirst && parseds.Count == 0)
                    {
                        return ParseResult<IEnumerable<T>>.Failure(cursol);
                    }

                    return ParseResult<IEnumerable<T>>.Success(
                        result.Cursol, parseds.ToArray());
                }

                if (result.Cursol.Index == current.Index)
                {
                    throw new InvalidOperationException("反復する要素パーサーが入力を消費せず成功しました。");
                }

                current = result.Cursol;
                parseds.Add(result.Parsed);
            }
        }
        finally
        {
            parseds.Dispose();
        }
    }
}
