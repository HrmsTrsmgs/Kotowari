using Marimo.Kotowari.Core;

namespace Marimo.Kotowari;

internal static class WhiteSpaceExecution
{
    // 空白解析が失敗時に入力を消費しても、その消費は採用せず失敗直前の位置を返す。
    internal static Cursol<TDocument> Skip<TDocument>(
        ParserExecution<char, TDocument> whiteSpace, Cursol<TDocument> current)
        where TDocument : IDocument, allows ref struct
    {
        while (true)
        {
            var result = whiteSpace.ParseCore(current);
            if (!result.IsSuccess) return current;

            current = result.Cursol;
        }
    }
}
