using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OrParser<T> : Parser<T>
    where T : notnull
{
    Parser<T>[] Parsers { get; }

    public OrParser(params Parser<T>[] parsers)
        => Parsers = parsers;

    protected internal override ParseResult<T, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<T, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(OrParser<T>)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    internal sealed class Execution<TDocument> : ParserExecution<T, TDocument>
        where TDocument : IDocument, allows ref struct
    {
        readonly Parser<T>[] parsers;
        readonly ParserExecution<T, TDocument>?[] executions;

        Execution(OrParser<T> parser) : base(parser)
        {
            parsers = parser.Parsers;
            executions = new ParserExecution<T, TDocument>?[parsers.Length];
        }

        internal static Execution<TDocument> Get(OrParser<T> parser)
            => ParserExecutionCache<OrParser<T>, Execution<TDocument>>.Get(
                parser, static it => new Execution<TDocument>(it));

        internal override ParseResult<T, TDocument> ParseCore(Cursol<TDocument> cursol)
        {
            for (var index = 0; index < parsers.Length; index++)
            {
                var parser = parsers[index];
                var execution = executions[index];

                // 元の候補配列は呼び出し側でも差し替え可能。到達した候補だけを構成する。
                if (execution is null || !ReferenceEquals(execution.Definition, parser))
                {
                    execution = parser.CreateExecution<TDocument>();
                    executions[index] = execution;
                }

                var result = execution.ParseCore(cursol);
                if (result.IsSuccess)
                {
                    return result;
                }
            }

            return ParseResult<T, TDocument>.Failure(cursol);
        }
    }
}
