using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ZeroOrMoreParser<T> : Parser<IEnumerable<T>>
{
    Parser<T> Parser { get; }

    public ZeroOrMoreParser(Parser<T> parser)
    {
        Parser = parser;
    }

    protected override (bool isSuccess, Cursol cursol, IEnumerable<T> parsed) ParseCore(Cursol cursol)
    {
        var parseds = new List<T> { };
        bool isSuccess;
        T parsed;
        var current = cursol;
        while (true)
        {
            (isSuccess, current, parsed) = Parser.Parse(current);
            if (!isSuccess)
            {
                return (true, current, parseds);
            }
            parseds.Add(parsed);
        }
    }
}
