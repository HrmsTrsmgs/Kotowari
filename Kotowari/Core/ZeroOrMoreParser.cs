using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ZeroOrMoreParser<T> : Parser<IEnumerable<T>>
    where T : notnull
{
    Parser<T> Parser { get; }

    public ZeroOrMoreParser(Parser<T> parser)
        => Parser = parser;

    protected internal override ParseResult<IEnumerable<T>, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<IEnumerable<T>, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(ZeroOrMoreParser<T>)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    abstract class Execution<TDocument>(ZeroOrMoreParser<T> definition)
        : ParserExecution<IEnumerable<T>, TDocument>(definition)
        where TDocument : IDocument, allows ref struct
    {
        internal static Execution<TDocument> Get(ZeroOrMoreParser<T> definition)
            => ParserExecutionCache<ZeroOrMoreParser<T>, Execution<TDocument>>.Get(definition, Create);

        static Execution<TDocument> Create(ZeroOrMoreParser<T> definition)
        {
            var parser = definition.Parser.CreateExecution<TDocument>();
            return parser is OrParser<T>.Execution<TDocument> orParser
                ? new Loop<OrChild>(definition, new OrChild(orParser))
                : new Loop<VirtualChild>(definition, new VirtualChild(parser));
        }

        interface IChild
        {
            ParseResult<T, TDocument> ParseCore(Cursol<TDocument> cursol);
        }

        readonly struct OrChild(OrParser<T>.Execution<TDocument> parser) : IChild
        {
            public ParseResult<T, TDocument> ParseCore(Cursol<TDocument> cursol)
                => parser.ParseCore(cursol);
        }

        readonly struct VirtualChild(ParserExecution<T, TDocument> parser) : IChild
        {
            public ParseResult<T, TDocument> ParseCore(Cursol<TDocument> cursol)
                => parser.ParseCore(cursol);
        }

        // 文字列のOrと数字列のConditionalCharを同じ反復メソッドの型profileへ集めない。
        // 値型の子adapterを型引数にして、反復本体を一つに保ちながら実行コードを分ける。
        // Orでは具体的なsealed実行器を呼び、他の子や利用側の派生型はvirtual呼び出しを保つ。
        sealed class Loop<TChild>(ZeroOrMoreParser<T> definition, TChild parser)
            : Execution<TDocument>(definition)
            where TChild : struct, IChild
        {
            internal override ParseResult<IEnumerable<T>, TDocument> ParseCore(Cursol<TDocument> cursol)
            {
                var parseds = new List<T> { };
                var current = cursol;
                while (true)
                {
                    var result = parser.ParseCore(current);
                    if (!result.IsSuccess)
                    {
                        return ParseResult<IEnumerable<T>, TDocument>.Success(result.Cursol, parseds);
                    }

                    current = result.Cursol;
                    parseds.Add(result.Parsed);
                }
            }
        }
    }
}
