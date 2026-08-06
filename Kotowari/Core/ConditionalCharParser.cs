using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari.Core;

public class ConditionalCharParser(Func<char, bool> condition) : Parser<char>
{
    protected override ParseResult<char> ParseCore(Cursol cursol)
        => cursol.Current switch
        {
            var c when (condition(c))
                    => ParseResult<char>.Success(cursol.GoFoward(1), c),
            _ => ParseResult<char>.Failure(cursol)
        };
}
