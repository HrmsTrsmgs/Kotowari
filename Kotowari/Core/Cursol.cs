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
        document = new StringDocument(text);
        Index = index;
    }

    Cursol(ReadOnlyMemory<byte> utf8)
    {
        document = new Utf8Document(utf8);
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
    abstract class Document
    {
        public abstract ReadOnlySpan<char> Text { get; }
        public abstract char this[int index] { get; }
        public abstract int GoFoward(int index, int step);
    }

    sealed class StringDocument(string text) : Document
    {
        readonly string input = text;

        public override ReadOnlySpan<char> Text => input;

        // 生成時に長さを取得し、null入力の既存の例外発生時点を保つ。
        public int Length { get; } = text.Length;

        public override char this[int index]
        {
            get
            {
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

        public override int GoFoward(int index, int step)
            => Math.Min(index + step, Length);
    }

    sealed class Utf8Document(ReadOnlyMemory<byte> input) : Document
    {
        readonly ReadOnlyMemory<byte> utf8 = input;

        public override ReadOnlySpan<char> Text => default;

        public int Length { get; } = input.Length;

        public override char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length) return NullCharacter;

                var remaining = utf8.Span[index..];
                Rune.DecodeFromUtf8(remaining, out var current, out var consumed);
                if (current.IsBmp) return (char)current.Value;

                // 補助平面文字は一文字のcharに収まらないため、UTF-16への変換で拒否する。
                Span<char> destination = stackalloc char[1];
                Encoding.UTF8.GetChars(remaining[..consumed], destination);
                return destination[0];
            }
        }

        public override int GoFoward(int index, int step)
        {
            while (step > 0 && index < Length)
            {
                index += Utf8CharacterLength(utf8.Span[index..]);
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
