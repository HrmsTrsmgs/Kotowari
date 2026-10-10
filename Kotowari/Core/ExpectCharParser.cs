using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ExpectCharParser(Parser<char> expectChars) : Parser<char>
{
    Parser<char> ExpectChars { get; } = expectChars;

    protected internal override ParseResult<char, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<char, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(ExpectCharParser)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    sealed class Execution<TDocument> : ParserExecution<char, TDocument>
        where TDocument : IDocument, allows ref struct
    {
        readonly ParserExecution<char, TDocument> expectChars;

        Execution(ExpectCharParser definition) : base(definition)
            => expectChars = definition.ExpectChars.CreateExecution<TDocument>();

        internal static Execution<TDocument> Get(ExpectCharParser definition)
            => ParserExecutionCache<ExpectCharParser, Execution<TDocument>>.Get(
                definition, static it => new Execution<TDocument>(it));

        internal override ParseResult<char, TDocument> ParseCore(Cursol<TDocument> cursol)
            => expectChars.ParseCore(cursol).IsSuccess
                ? ParseResult<char, TDocument>.Failure(cursol)
                : ParseResult<char, TDocument>.Success(cursol.GoFoward(1), cursol.Current);
    }
}
