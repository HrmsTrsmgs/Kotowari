using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Marimo.Kotowari;

public class WithWhiteSpaceParser<T> : Parser<T>
    where T : notnull
{
    Parser<char> WhiteSpace { get; }
    Parser<T> Parser { get; }
    public WithWhiteSpaceParser(Parser<char> whiteSpace, Parser<T> parser)
    {
        WhiteSpace = whiteSpace;
        Parser = parser;
    }

    protected internal override ParseResult<T, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<T, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(WithWhiteSpaceParser<T>)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    sealed class Execution<TDocument> : ParserExecution<T, TDocument>
        where TDocument : IDocument, allows ref struct
    {
        readonly ParserExecution<char, TDocument> whiteSpace;
        readonly ParserExecution<T, TDocument> parser;

        Execution(WithWhiteSpaceParser<T> definition) : base(definition)
        {
            whiteSpace = definition.WhiteSpace.CreateExecution<TDocument>();
            parser = definition.Parser.CreateExecution<TDocument>();
        }

        internal static Execution<TDocument> Get(WithWhiteSpaceParser<T> definition)
            => ParserExecutionCache<WithWhiteSpaceParser<T>, Execution<TDocument>>.Get(
                definition, static it => new Execution<TDocument>(it));

        internal override ParseResult<T, TDocument> ParseCore(Cursol<TDocument> cursol)
            => parser.ParseCore(WhiteSpaceExecution.Skip(whiteSpace, cursol));
    }
}
