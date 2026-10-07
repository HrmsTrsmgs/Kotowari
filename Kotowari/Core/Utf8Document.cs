using System;
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
    readonly ReadOnlySpan<byte> utf8;
    readonly object identity;

    internal Utf8Document(ReadOnlySpan<byte> utf8)
    {
        this.utf8 = utf8;
        identity = new object();
    }

    public object? Identity => identity;

    public ReadOnlySpan<char> Text => default;

    public char Read(int index)
    {
        if (index < 0 || index >= utf8.Length) return Cursol.NullCharacter;

        var remaining = utf8[index..];
        Rune.DecodeFromUtf8(remaining, out var current, out var consumed);
        if (current.IsBmp) return (char)current.Value;

        // 補助平面文字は一文字のcharに収まらないため、UTF-16への変換で拒否する。
        Span<char> destination = stackalloc char[1];
        Encoding.UTF8.GetChars(remaining[..consumed], destination);
        return destination[0];
    }

    public int GoFoward(int index, int step)
    {
        while (step > 0 && index < utf8.Length)
        {
            index += Utf8CharacterLength(utf8[index..]);
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
