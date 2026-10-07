using System;

namespace Marimo.Kotowari.Core;

/// <summary>
/// UTF-16の文字列を保持し、char単位の位置で読み取る文書。
/// </summary>
/// <remarks>
/// 文書の型を値型にして、カーソルの型引数で入力操作を分離します。
/// 入力と文書の同一性は一つの共有参照に保持するため、コピーで新たな文書を生成せず、
/// 文字列用カーソルの入力状態にもその参照一つだけを保持します。
/// 通常はCursol.FromStringでカーソルを作成します。
/// </remarks>
public readonly struct StringDocument : IDocument
{
    readonly Input input;

    internal StringDocument(string text)
        => input = new Input(text);

    public object? Identity => input;

    public ReadOnlySpan<char> Text => input is null ? default : input.Text;

    public char Read(int index)
    {
        try
        {
            return input.Text[index];
        }
        catch (IndexOutOfRangeException)
        {
            return Cursol.NullCharacter;
        }
    }

    public int GoFoward(int index, int step)
        => Math.Min(index + step, input.Length);

    // 同じstringから作成した別文書を区別し、コピーには同じ入力を共有させる。
    sealed class Input(string text)
    {
        public string Text { get; } = text;

        // 生成時に長さを取得し、null入力の既存の例外発生時点を保つ。
        public int Length { get; } = text.Length;
    }
}
