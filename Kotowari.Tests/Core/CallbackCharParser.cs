using Marimo.Kotowari.Core;

using System;

namespace Marimo.Kotowari.Tests.Core;

// 外部派生型のTryParseCoreを通ることを観測する。
internal sealed class CallbackCharParser(Func<char, int, (bool IsSuccess, char Parsed, int Step)> parse) : Parser<char>
{
    protected override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        var (isSuccess, value, step) = parse(start.Current, start.Index);
        next = start.GoFoward(step);
        parsed = value;
        return isSuccess;
    }
}
