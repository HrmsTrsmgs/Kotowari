using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Marimo.Kotowari.Core;

public class DelimitedSequenceParser<T, U> : Parser<IEnumerable<T>>
{
    Parser<T> Sequence { get; }
    Parser<U> Delimiter { get; }
    public DelimitedSequenceParser(Parser<T> sequence, Parser<U> delimiter)
    {
        Sequence = sequence;
        Delimiter = delimiter;
    }

    /// <summary>要素と区切りを交互に解析します。</summary>
    /// <exception cref="InvalidOperationException">要素と区切りの一巡で入力位置が進まずに成功した場合にスローされます。</exception>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out IEnumerable<T> parsed)
    {
        // 短い列ではList本体と成長途中の配列を確保せず、結果だけを独立した配列にする。
        // 結果に借用領域を含めず、子パーサーが例外を投げた場合もfinallyで返却する。
        // 測定・採否: Kotowari.Benchmarks/Results/2026-10-19-string-tuning.md。
        var scratch = new ParsedValueScratchBuffer<T>();
        var parseds = new ParsedValueBuffer<T>(scratch);
        try
        {
            var current = start;
            var beforeDelimiter = current;
            while (true)
            {
                var beforeSequence = current;
                if (!Sequence.TryParseCore(current, out var sequenceNext, out var sequenceParsed))
                {
                    next = beforeDelimiter;
                    parsed = parseds.ToArray();
                    return true;
                }

                current = sequenceNext;
                parseds.Add(sequenceParsed);
                beforeDelimiter = current;
                if (!Delimiter.TryParseCore(current, out var delimiterNext, out _))
                {
                    next = beforeDelimiter;
                    parsed = parseds.ToArray();
                    return true;
                }

                current = delimiterNext;
                if (current.Index == beforeSequence.Index)
                {
                    throw new InvalidOperationException("反復する要素と区切りパーサーが入力を消費せず成功しました。");
                }
            }
        }
        finally
        {
            parseds.Dispose();
        }
    }
}
