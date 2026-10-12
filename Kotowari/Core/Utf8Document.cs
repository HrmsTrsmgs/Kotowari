using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Marimo.Kotowari.Core;

/// <summary>
/// UTF-8入力をコピーせず借用し、バイト単位の位置で読み取る文書。
/// </summary>
/// <remarks>
/// 有効なASCII・BMP文字のみを扱います。入力の有効期間内で同期的に使用してください。
/// 借用状態はこの文書だけに保持し、文字列文書のカーソルや読み取り引数へ持ち込みません。
/// 通常はCursol.FromUtf8でカーソルを作成します。
/// </remarks>
public readonly ref struct Utf8Document : IDocument
{
    // 同じSpanから取得した参照と長さを変更せず保持し、借用範囲を維持する。
    readonly ref readonly byte utf8Reference;
    readonly int utf8Length;
    readonly object identity;

    /// <remarks>
    /// 入力の参照と長さを直接保持し、読み取り時だけSpanとして扱う。
    /// Spanをフィールドに保持する形より低水準だが、型別カーソルを通るJITコードが変わり、
    /// .NET 10・PGO有効のJSON全体測定で改善を確認したため、この表現を使用している。
    /// すべてのカーソルコピーが消えるわけではなく、個々の命令の寄与も未分離。
    /// 同じSpan由来の参照と長さを不変に保ち、借用期間を延ばしたり長さを合成したりしない。
    /// Spanフィールドへ戻す場合やランタイム更新時には、全体測定で再評価する。
    /// 根拠と条件はKotowari.Benchmarks/Results/2026-10-10-utf8-document-layout.mdを参照。
    /// </remarks>
    ReadOnlySpan<byte> Utf8
        => MemoryMarshal.CreateReadOnlySpan(in utf8Reference, utf8Length);

    internal Utf8Document(ReadOnlySpan<byte> utf8)
    {
        utf8Reference = ref MemoryMarshal.GetReference(utf8);
        utf8Length = utf8.Length;
        identity = new object();
    }

    public object? Identity => identity;

    public ReadOnlySpan<char> Text => default;

    public char Read(int index)
    {
        if (index < 0 || index >= utf8Length) return Cursol.NullCharacter;

        var remaining = Utf8[index..];
        Rune.DecodeFromUtf8(remaining, out var current, out var consumed);
        if (current.IsBmp) return (char)current.Value;

        return RejectSupplementaryCharacter(remaining[..consumed]);
    }

    // stackallocによるinline拒否が通常の読み取りへ影響しないよう、非BMPの拒否処理を分離する。
    static char RejectSupplementaryCharacter(ReadOnlySpan<byte> utf8)
    {
        // 補助平面文字は一文字のcharに収まらないため、UTF-16への変換で拒否する。
        Span<char> destination = stackalloc char[1];
        Encoding.UTF8.GetChars(utf8, destination);
        return destination[0];
    }

    public int GoFoward(int index, int step)
    {
        while (step > 0 && index < utf8Length)
        {
            index += Utf8CharacterLength(Utf8[index..]);
            step--;
        }
        return index;
    }

    static int Utf8CharacterLength(ReadOnlySpan<byte> bytes)
    {
        Rune.DecodeFromUtf8(bytes, out _, out var consumed);
        return consumed;
    }
}
