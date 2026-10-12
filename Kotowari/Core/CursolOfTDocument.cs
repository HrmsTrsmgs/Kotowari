using System;

using System.Runtime.CompilerServices;

namespace Marimo.Kotowari.Core;


/// <summary>
/// 入力型に対応する文書と読み取り位置を表す、同期解析用のカーソル。
/// </summary>
/// <typeparam name="TDocument">入力の保持と位置の解釈を担う文書型。</typeparam>
/// <remarks>
/// 文書をinterface変数へ変換せず、具体的な型のまま保持します。
/// 文字列用カーソルにはUTF-8入力の参照や長さを持たせません。
/// UTF-8入力を借用する場合、カーソルとそのコピーは入力の有効期間内で使用してください。
/// ヒープへの格納、boxing、ラムダでの捕捉はできません。
/// </remarks>
public readonly ref struct Cursol<TDocument>
    where TDocument : IDocument, allows ref struct
{
    readonly TDocument document;

    public char Current => document.Read(Index);

    public int Index { get; private init; }
    public ReadOnlySpan<char> Text => document.Text;

    internal Cursol(TDocument document)
    {
        this.document = document;
        Index = 0;
    }

    public Cursol<TDocument> GoFoward(int step)
        => this with { Index = document.GoFoward(Index, step) };

    public Cursol<TDocument> Copy() => this with { };

    /// <summary>
    /// UTF-8文書の場合だけ、現在位置以降を直接照合するために借用します。
    /// </summary>
    /// <remarks>
    /// ref structはboxingによる型変換ができないため、厳密な型一致を確認して参照を変換します。
    /// 異なる型のレイアウトを仮定せず、文書を書き換えたり借用期間を延長したりしません。
    /// 文字列や外部の文書型は通常の文字読み取りを使用します。
    /// </remarks>
    internal bool TryGetUtf8(out ReadOnlySpan<byte> remaining)
    {
        if (typeof(TDocument) == typeof(Utf8Document))
        {
            ref readonly var utf8 = ref Unsafe.As<TDocument, Utf8Document>(ref Unsafe.AsRef(in document));
            var bytes = utf8.Utf8;
            if ((uint)Index <= (uint)bytes.Length)
            {
                remaining = bytes[Index..];
                return true;
            }
        }

        remaining = default;
        return false;
    }

    // TryGetUtf8で借用した範囲の直接照合に成功した場合に、その消費バイト数だけ進む。
    // 文字数を再走査せず、文書の同一性と借用期間は元のカーソルを引き継ぐ。
    internal Cursol<TDocument> AdvanceUtf8Bytes(int count)
        => this with { Index = Index + count };

    /// <summary>
    /// 同じ文書の同じ位置を指すかを比較します。入力内容そのものは比較しません。
    /// </summary>
    public bool Equals(Cursol<TDocument> other)
        => ReferenceEquals(document.Identity, other.document.Identity) && Index == other.Index;

    /// <summary>
    /// boxingできないため、objectとの比較は常にfalseを返します。
    /// </summary>
    public override bool Equals(object? obj) => false;

    /// <summary>
    /// 文書の同一性と位置に基づくハッシュ値を返します。
    /// </summary>
    public override int GetHashCode()
        => HashCode.Combine(document.Identity, Index);

    public static bool operator ==(Cursol<TDocument> left, Cursol<TDocument> right)
        => left.Equals(right);

    public static bool operator !=(Cursol<TDocument> left, Cursol<TDocument> right)
        => !left.Equals(right);
}
