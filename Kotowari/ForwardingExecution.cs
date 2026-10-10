using Marimo.Kotowari.Core;

namespace Marimo.Kotowari;

// 転送元の定義を保持し、Orの候補配列との差し替え判定に子の定義を使わせない。
// 固定した子の実行器はキャッシュのfactoryで一度構成し、解析では転送だけを行う。
internal sealed class ForwardingExecution<T, TDocument>(
    Parser<T> definition, ParserExecution<T, TDocument> parser)
    : ParserExecution<T, TDocument>(definition)
    where T : notnull
    where TDocument : IDocument, allows ref struct
{
    internal override ParseResult<T, TDocument> ParseCore(Cursol<TDocument> cursol)
        => parser.ParseCore(cursol);
}
