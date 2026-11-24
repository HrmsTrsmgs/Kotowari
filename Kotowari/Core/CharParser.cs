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

    protected internal override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        var c = start.Current;
        if (IgnoreCase ? char.ToLower(c) == char.ToLower(Char) : c == Char)
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
