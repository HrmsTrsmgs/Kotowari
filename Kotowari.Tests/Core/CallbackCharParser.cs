using Marimo.Kotowari.Core;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Marimo.Kotowari.Tests.Core;

// 継承元ごとのParseCoreを呼ぶことで、外部派生型のフォールバックを観測する。
internal sealed class CallbackCharParser(Func<char, int, (bool IsSuccess, char Parsed, int Step)> parse) : Parser<char>
{
    protected override ParseResult<char, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var (isSuccess, parsed, step) = parse(cursol.Current, cursol.Index);
        return isSuccess
            ? ParseResult<char, TDocument>.Success(cursol.GoFoward(step), parsed)
            : ParseResult<char, TDocument>.Failure(cursol.GoFoward(step));
    }
}
