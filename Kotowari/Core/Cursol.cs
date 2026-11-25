using System;

namespace Marimo.Kotowari.Core;

/// <summary>一つの入力文書とその読み取り位置を表します。同じ文書を指すカーソルは位置で区別されます。</summary>
public readonly record struct Cursol
{
    /// <summary>
    /// 範囲外の文字読み取りで返すヌル文字（U+0000）。
    /// </summary>
    public const char NullCharacter = '\0';

    readonly Document document;

    /// <summary>現在位置の文字。初期化したカーソルでは、終端を含む範囲外の位置でヌル文字を返します。</summary>
    public char Current => document[Index];

    /// <summary>文書の先頭を0とする位置。負の移動量で先頭より前に移動することもできます。</summary>
    public int Index { get; private init; }

    /// <summary>現在位置以降ではなく、文書全体の文字を返します。</summary>
    public ReadOnlySpan<char> Text => document is null ? default : document.Text;

    /// <summary>新しい文書の先頭に位置するカーソルを作ります。同じ文字列から別々に作った文書は区別されます。</summary>
    /// <param name="text">読み取る文字列。</param>
    public Cursol(string text) : this(text, 0)
    { }
    Cursol(string text, int index)
    {
        document = new(text);
        Index = index;
    }

    /// <summary>元のカーソルを変更せず、同じ文書内を相対移動します。</summary>
    /// <param name="step">現在位置からの移動量。正方向の移動は文書の末尾で止まります。</param>
    /// <returns>移動後のカーソル。</returns>
    public Cursol GoFoward(int step)
        => this with { Index = document.GoFoward(Index, step) };

    /// <summary>同じ文書の同じ位置を指すカーソルを返します。</summary>
    public Cursol Copy() => this with { };

    /// <summary>
    /// 入力文字列の読み取り・位置計算と、Cursol間で共有する文書の同一性を担う。
    /// </summary>
    sealed class Document(string text)
    {
        readonly string input = text;

        /// <summary>どの位置のカーソルからも同じ文書を参照できます。</summary>
        public ReadOnlySpan<char> Text => input;

        /// <summary>生成時に長さを取得し、null 入力の既存の例外発生時点を保ちます。</summary>
        public int Length { get; } = text.Length;

        /// <summary>負の位置と終端以降を、例外を経由せず同じヌル文字として扱います。</summary>
        /// <remarks>読み取り時の例外処理を避ける判断と測定は 2026-10-18-string-tuning.md を参照してください。</remarks>
        public char this[int index]
            => (uint)index < (uint)input.Length
                ? input[index]
                : NullCharacter;

        /// <summary>現在位置からの移動量を適用し、文書の終端を越えない位置を返します。</summary>
        public int GoFoward(int index, int step)
        {
            if (step > 0)
            {
                // 正方向は加算時のオーバーフローを避けてから文書末尾に収める。
                return (int)Math.Min((long)index + step, Length);
            }

            return Math.Min(index + step, Length);
        }
    }
}
