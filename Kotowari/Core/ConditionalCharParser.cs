using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari.Core;

public class ConditionalCharParser : Parser<char>
{
    Func<char, bool> Condition { get; }

    public ConditionalCharParser(Func<char, bool> condition)
        => Condition = condition;

    protected override ParseResult<char> ParseCore(Cursol cursol)
        => cursol.Current switch
        {
            var c when (Condition(c))
                    => ParseResult<char>.Success(cursol.GoFoward(1), c),
            _ => ParseResult<char>.Failure(cursol)
        };
}
