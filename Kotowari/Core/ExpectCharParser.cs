using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ExpectCharParser(Parser<char> expectChars) : Parser<char>
{
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        if (expectChars.TryParseCore(start, out _, out _))
        {
            next = start;
            parsed = default;
            return false;
        }

        next = start.GoFoward(1);
        parsed = start.Current;
        return true;
    }
}
