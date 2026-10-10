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
