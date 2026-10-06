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

    public Cursol GoFoward(int step)
        => this with { Index = document.GoFoward(Index, step) };

    public Cursol Copy() => this with { };

    /// <summary>
    /// 入力文字列の読み取り・位置計算と、Cursol間で共有する文書の同一性を担う。
    /// </summary>
    sealed class Document(string text)
    {
        readonly string input = text;

        public ReadOnlySpan<char> Text => input;

        // 生成時に長さを取得し、null入力の既存の例外発生時点を保つ。
        public int Length { get; } = text.Length;

        public char this[int index]
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

        public int GoFoward(int index, int step)
            => Math.Min(index + step, Length);
    }
}
