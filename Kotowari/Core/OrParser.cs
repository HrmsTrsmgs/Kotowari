using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OrParser<T> : Parser<T>
{
    Parser<T>[] Parsers { get; }

    public OrParser(params Parser<T>[] parsers)
    {
        Parsers = parsers;
    }

    protected override (bool isSuccess, Cursol cursol, T parsed) ParseCore(Cursol cursol)
    {
        foreach(var parser in Parsers)
        {
            var result = parser.Parse(cursol);

            if(result.isSuccess)
            {
                return result;
            }
        }
        return (false, cursol, default);
    }
}
