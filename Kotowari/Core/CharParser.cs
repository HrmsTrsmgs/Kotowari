using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class CharParser : Parser<char>
{
    char Char { get; }

    bool IgnoreCase { get; }

    public CharParser(char @char, bool ignoreCase = false)
    {
        Char = @char;
        IgnoreCase = ignoreCase;
    }

    protected internal override ParseResult<char, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => ParseCharacter(cursol);

    internal override ParserExecution<char, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(CharParser)
            ? new Execution<TDocument>(this)
            : base.CreateExecution<TDocument>();

    ParseResult<char, TDocument> ParseCharacter<TDocument>(Cursol<TDocument> cursol)
        where TDocument : IDocument, allows ref struct
        => cursol.Current switch
        {
            var c when (IgnoreCase ? char.ToLower(c) == char.ToLower(Char) : c == Char)
                    => ParseResult<char, TDocument>.Success(cursol.GoFoward(1), c),
            _ => ParseResult<char, TDocument>.Failure(cursol)
        };

    sealed class Execution<TDocument>(CharParser parser) : ParserExecution<char, TDocument>(parser)
        where TDocument : IDocument, allows ref struct
    {
        internal override ParseResult<char, TDocument> ParseCore(Cursol<TDocument> cursol)
            => parser.ParseCharacter(cursol);
    }
}
