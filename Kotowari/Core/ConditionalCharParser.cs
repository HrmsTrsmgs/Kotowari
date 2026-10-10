using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari.Core;

public class ConditionalCharParser(Func<char, bool> condition) : Parser<char>
{
    protected internal override ParseResult<char, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => ParseCharacter(cursol);

    internal override ParserExecution<char, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(ConditionalCharParser)
            ? new Execution<TDocument>(this)
            : base.CreateExecution<TDocument>();

    ParseResult<char, TDocument> ParseCharacter<TDocument>(Cursol<TDocument> cursol)
        where TDocument : IDocument, allows ref struct
        => cursol.Current switch
        {
            var c when (condition(c))
                    => ParseResult<char, TDocument>.Success(cursol.GoFoward(1), c),
            _ => ParseResult<char, TDocument>.Failure(cursol)
        };

    sealed class Execution<TDocument>(ConditionalCharParser definition) : ParserExecution<char, TDocument>(definition)
        where TDocument : IDocument, allows ref struct
    {
        internal override ParseResult<char, TDocument> ParseCore(Cursol<TDocument> cursol)
            => definition.ParseCharacter(cursol);
    }
}
