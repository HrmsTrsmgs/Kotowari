using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ExpectCharParser : Parser<char>
{
    Parser<char> ExpectChars { get; }

    public ExpectCharParser(Parser<char> expectChars)
        => ExpectChars = expectChars;

    protected override ParseResult<char> ParseCore(Cursol cursol)
        => ExpectChars.Parse(cursol).IsSuccess
            ? ParseResult<char>.Failure(cursol)
            : ParseResult<char>.Success(cursol.GoFoward(1), cursol.Current);
}
