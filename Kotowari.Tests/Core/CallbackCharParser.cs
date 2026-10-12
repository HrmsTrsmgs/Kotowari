using Marimo.Kotowari.Core;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Marimo.Kotowari.Tests.Core;

// 継承元ごとのParseCoreを呼ぶことで、外部派生型のフォールバックを観測する。
internal sealed class CallbackCharParser(Func<char, int, (bool IsSuccess, char Parsed, int Step)> parse) : Parser<char>
{
    protected override ParseResult<char> ParseCore(Cursol cursol)
    {
        var (isSuccess, parsed, step) = parse(cursol.Current, cursol.Index);
        return isSuccess
            ? ParseResult<char>.Success(cursol.GoFoward(step), parsed)
            : ParseResult<char>.Failure(cursol.GoFoward(step));
    }
}
