using Marimo.Kotowari.Core;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Marimo.Kotowari.Tests.Core;

// 継承元ごとのParseCoreを呼ぶことで、外部派生型のフォールバックを観測する。
internal sealed class UpperCaseCharParser() : CharParser('a')
{
    public int 呼び出し回数 { get; private set; }

    protected override ParseResult<char> ParseCore(Cursol cursol)
    {
        呼び出し回数++;
        return TestParseResults.ConvertSuccessfulValue(base.ParseCore(cursol), char.ToUpper);
    }
}
