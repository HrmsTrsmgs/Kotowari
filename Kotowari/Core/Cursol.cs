using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Core;

public readonly record struct Cursol
{
    public const char Null = '\0';

    readonly Document document;

    public char Current
    {
        get
        {

            try
            {
                return document.Text[Index];
            }
            catch (IndexOutOfRangeException)
            {
                return Null;
            }
        }
    }

    public int Index { get; private init; }
    public ReadOnlySpan<char> Text => document?.Text;
    public Cursol(string text) : this(text, 0)
    {}
    Cursol(string text, int index)
    {
        document = new(text);
        Index = index;
    }

    public Cursol GoFoward(int step)
        => this with { Index = Math.Min(Index + step, document.Length) };

    public Cursol Copy() => this with { };

    /// <summary>
    /// 入力文字列を保持し、Cursol間で共有する文書の同一性を表す。
    /// </summary>
    /// <remarks>
    /// 文書の同一性はこのオブジェクトの参照で表し、Cursolの等価性は文書と位置で決まる。
    /// コピーや移動で生成したCursolは同じDocumentを共有し、位置も同じなら等価になる。
    /// 一方、同じstringから別々に生成したCursolは異なるDocumentを持ち、別文書として扱う。
    /// stringを直接フィールドに持つと内容による等価比較になり、この区別を失う。
    /// 文書を参照で識別するため、record化や内容によるEqualsの実装は行わない。
    /// </remarks>
    sealed class Document(string text)
    {
        public string Text { get; } = text;

        // 生成時に長さを取得し、null入力の既存の例外発生時点を保つ。
        public int Length { get; } = text.Length;
    }
}
