using System;

namespace Marimo.Kotowari.Core;

/// <summary>
/// カーソルが文書型を保ったまま行う、入力の読み取りと位置計算の契約。
/// </summary>
/// <remarks>
/// 入力の保存形式と位置の単位は文書型が決めます。
/// UTF-8文書はref structなので、IDocument型の変数へ変換せず、
/// IDocument制約とallows ref structを指定した型引数として使用します。
/// </remarks>
public interface IDocument
{
    /// <summary>
    /// 入力内容ではなく、文書の同一性を表す参照。コピーでは同じ参照を保持します。
    /// </summary>
    object? Identity { get; }

    /// <summary>
    /// 文字列文書では全文、UTF-8文書では空のSpanを返します。
    /// </summary>
    ReadOnlySpan<char> Text { get; }

    /// <summary>
    /// 指定位置の文字を読み取り、入力範囲外ではヌル文字を返します。
    /// </summary>
    char Read(int index);

    /// <summary>
    /// 指定位置から文字数だけ進んだ位置を、文書固有の単位で返します。
    /// </summary>
    int GoFoward(int index, int step);
}
