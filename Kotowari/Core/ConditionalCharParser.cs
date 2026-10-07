using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari.Core;

public class ConditionalCharParser(Func<char, bool> condition) : Parser<char>
{
    protected internal override ParseResult<char, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => cursol.Current switch
        {
            var c when (condition(c))
                    => ParseResult<char, TDocument>.Success(cursol.GoFoward(1), c),
            _ => ParseResult<char, TDocument>.Failure(cursol)
        };
}
