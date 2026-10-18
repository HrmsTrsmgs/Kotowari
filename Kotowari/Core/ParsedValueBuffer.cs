using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Marimo.Kotowari.Core;

/// <summary>反復解析の一時値を集め、借用領域と独立した結果配列を作る。</summary>
/// <remarks>
/// 呼び出し側の小さな領域から始め、不足した場合だけ配列を借りて拡張する。
/// ToArrayは確定した値を所有する新しい配列を返し、借用した配列を外部へ渡さない。
/// 子パーサーや結果配列の生成で例外が出る場合も、呼び出し側のfinallyでDisposeする。
/// 参照を含む配列は返却時にクリアし、解析結果をpoolに保持させない。
/// Listへの単純化は、成長途中の配列とList本体の確保を再導入するため性能を再確認する。
/// 文字列専用版・PGO有効・MemoryStreamから巨大JSONを解析する独立確認では、
/// List版より約3.1%短縮し、割り当ても約64.50MBから55.34MBへ減った。
/// 全入力やTでの優位を保証するものではなく、固定8要素の一時領域もTの大きさに依存する。
/// 条件・全測定値・採用理由: Kotowari.Benchmarks/Results/2026-10-18-string-tuning.md。
/// </remarks>
ref struct ParsedValueBuffer<T>
{
    Span<T> buffer;
    T[]? rented;
    int count;

    internal ParsedValueBuffer(Span<T> initialBuffer)
        => buffer = initialBuffer;

    internal void Add(T value)
    {
        if (count == buffer.Length)
        {
            Grow();
        }

        buffer[count++] = value;
    }

    internal readonly T[] ToArray()
        => [.. buffer[..count]];

    internal void Dispose()
    {
        if (rented is not null)
        {
            ArrayPool<T>.Shared.Return(rented, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }

        rented = null;
        buffer = default;
        count = 0;
    }

    void Grow()
    {
        var capacity = (int)Math.Min(Math.Max(16L, buffer.Length * 2L), Array.MaxLength);
        if (capacity <= count)
        {
            throw new OutOfMemoryException();
        }

        var next = ArrayPool<T>.Shared.Rent(capacity);
        buffer.CopyTo(next);
        if (rented is not null)
        {
            ArrayPool<T>.Shared.Return(rented, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }

        rented = next;
        buffer = next;
    }
}
