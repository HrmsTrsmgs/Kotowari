using Marimo.Kotowari.Core;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Marimo.Kotowari.Tests.Core;

// 継承元ごとのParseCoreを呼ぶことで、外部派生型のフォールバックを観測する。
internal sealed class ConsumingFailureCharParser : Parser<char>
{
    protected override ParseResult<char> ParseCore(Cursol cursol)
        => ParseResult<char>.Failure(cursol.GoFoward(2));
}
