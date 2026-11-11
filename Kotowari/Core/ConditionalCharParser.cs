using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari.Core;

public class ConditionalCharParser(Func<char, bool> condition) : Parser<char>
{
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        var c = start.Current;
        if (condition(c))
        {
            next = start.GoFoward(1);
            parsed = c;
            return true;
        }

        next = start;
        parsed = default;
        return false;
    }
}
