using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class RecursiveParser<T>(Func<Parser<T>> parserGetter) : Parser<T>
    where T : notnull
{
    Func<Parser<T>> ParserGetter { get; } = parserGetter;

    protected internal override ParseResult<T, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<T, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(RecursiveParser<T>)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    sealed class Execution<TDocument> : ParserExecution<T, TDocument>
        where TDocument : IDocument, allows ref struct
    {
        readonly Func<Parser<T>> parserGetter;

        Execution(RecursiveParser<T> definition) : base(definition)
            => parserGetter = definition.ParserGetter;

        internal static Execution<TDocument> Get(RecursiveParser<T> definition)
            => ParserExecutionCache<RecursiveParser<T>, Execution<TDocument>>.Get(
                definition, static it => new Execution<TDocument>(it));

        // getterの評価時点・回数と、その都度返された定義を維持する。
        // 固定の再帰先だとは仮定せず、返された動的な定義の入口から解析する。
        internal override ParseResult<T, TDocument> ParseCore(Cursol<TDocument> cursol)
            => parserGetter().ParseDynamicCore(cursol);
    }
}
