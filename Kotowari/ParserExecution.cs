using Marimo.Kotowari.Core;

namespace Marimo.Kotowari;

/// <summary>
/// 文法定義を保ったまま、入力型に対応する子パーサー呼び出しを実行します。
/// </summary>
/// <remarks>
/// 入力型をクラスの型引数へ移し、反復中のParseCoreを非ジェネリックvirtualにします。
/// 実行器は文法と子の実行器だけを保持し、借用した入力やカーソルをヒープへ保存しません。
/// 子の実行器は解析前または候補の初回使用時に構成し、文字ごとには生成しません。
/// </remarks>
internal abstract class ParserExecution<T, TDocument>(Parser<T> definition)
    where T : notnull
    where TDocument : IDocument, allows ref struct
{
    internal Parser<T> Definition { get; } = definition;

    internal abstract ParseResult<T, TDocument> ParseCore(Cursol<TDocument> cursol);
}
