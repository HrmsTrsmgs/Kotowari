using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Core;

public readonly record struct Cursol
{
    /// <summary>
    /// 範囲外の文字読み取りで返すヌル文字（U+0000）。
    /// </summary>
    public const char NullCharacter = '\0';

    readonly Document document;

    public char Current => document[Index];

    public int Index { get; private init; }
    public ReadOnlySpan<char> Text => document is null ? default : document.Text;
    public Cursol(string text) : this(text, 0)
    {}
    Cursol(string text, int index)
    {
        document = new(text);
        Index = index;
    }

    Cursol(ReadOnlyMemory<byte> utf8)
    {
        document = new(utf8);
        Index = 0;
    }

    /// <summary>
    /// 有効なUTF-8のASCII・BMP文字を読む先頭位置のカーソルを作成します。
    /// </summary>
    /// <param name="utf8">コピーせず保持するUTF-8バイト列。</param>
    /// <returns>Indexが入力先頭からのバイト位置を表すカーソル。</returns>
    /// <remarks>
    /// 正のGoFowardのstepは読み取る文字数を指定し、負のstepでは位置が変わりません。
    /// Textは空のSpanを返します。入力の基になるメモリを呼び出し側で変更すると読み取りに反映されます。
    /// 補助平面文字および不正なUTF-8入力の読み取りには対応していません。
    /// </remarks>
    public static Cursol FromUtf8(ReadOnlyMemory<byte> utf8)
        => new(utf8);

    public Cursol GoFoward(int step)
        => this with { Index = document.GoFoward(Index, step) };

    public Cursol Copy() => this with { };

    /// <summary>
    /// 入力の読み取り・位置計算と、Cursol間で共有する文書の同一性を担う。
    /// </summary>
    sealed class Document(string text)
    {
        readonly string input = text;
        readonly ReadOnlyMemory<byte>? utf8;

        public Document(ReadOnlyMemory<byte> utf8) : this(string.Empty)
        {
            this.utf8 = utf8;
            Length = utf8.Length;
        }

        public ReadOnlySpan<char> Text => input;

        // 生成時に長さを取得し、null入力の既存の例外発生時点を保つ。
        public int Length { get; } = text.Length;

        public char this[int index]
        {
            get
            {
                if (utf8 is { } bytes)
                {
                    if (index < 0 || index >= Length) return NullCharacter;

                    var remaining = bytes.Span[index..];
                    Span<char> current = stackalloc char[1];
                    Encoding.UTF8.GetChars(remaining[..Utf8CharacterLength(remaining)], current);
                    return current[0];
                }

                try
                {
                    return input[index];
                }
                catch (IndexOutOfRangeException)
                {
                    return NullCharacter;
                }
            }
        }

        public int GoFoward(int index, int step)
        {
            if (utf8 is not { } bytes) return Math.Min(index + step, Length);

            while (step > 0 && index < Length)
            {
                index += Utf8CharacterLength(bytes.Span[index..]);
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
}
