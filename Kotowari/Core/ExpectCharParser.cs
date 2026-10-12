using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ExpectCharParser(Parser<char> expectChars) : Parser<char>
{
    protected internal override ParseResult<char> ParseCore(Cursol cursol)
        => expectChars.ParseCore(cursol).IsSuccess
            ? ParseResult<char>.Failure(cursol)
            : ParseResult<char>.Success(cursol.GoFoward(1), cursol.Current);
}
