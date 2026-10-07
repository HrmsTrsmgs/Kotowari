using System;

namespace Marimo.Kotowari.Core;

/// <summary>
/// 入力の種類に対応する、先頭位置のカーソルを作成します。
/// </summary>
public static class Cursol
{
    /// <summary>
    /// 範囲外の文字読み取りで返すヌル文字（U+0000）。
    /// </summary>
    public const char NullCharacter = '\0';

    /// <summary>
    /// 文字列を読む先頭位置のカーソルを作成します。
    /// </summary>
    public static Cursol<StringDocument> FromString(string text)
        => new(new StringDocument(text));

    /// <summary>
    /// 有効なUTF-8のASCII・BMP文字を読む先頭位置のカーソルを作成します。
    /// </summary>
    /// <param name="utf8">コピーせず借用するUTF-8バイト列。</param>
    /// <returns>Indexが入力先頭からのバイト位置を表すカーソル。</returns>
    /// <remarks>
    /// 正のGoFowardのstepは読み取る文字数を指定し、負のstepでは位置が変わりません。
    /// Textは空のSpanを返します。入力の基になるメモリを呼び出し側で変更すると読み取りに反映されます。
    /// 返されたカーソルと解析結果は、入力の有効期間を越えて保持できません。
    /// 入力を同期解析中に変更しないでください。
    /// 補助平面文字および不正なUTF-8入力の読み取りには対応していません。
    /// </remarks>
    public static Cursol<Utf8Document> FromUtf8(ReadOnlySpan<byte> utf8)
        => new(new Utf8Document(utf8));
}

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
