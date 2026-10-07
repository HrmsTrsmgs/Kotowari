using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ExpectCharParser(Parser<char> expectChars) : Parser<char>
{
    protected internal override ParseResult<char, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => expectChars.ParseCore(cursol).IsSuccess
            ? ParseResult<char, TDocument>.Failure(cursol)
            : ParseResult<char, TDocument>.Success(cursol.GoFoward(1), cursol.Current);
}
