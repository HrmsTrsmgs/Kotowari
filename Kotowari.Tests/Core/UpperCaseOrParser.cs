using Marimo.Kotowari.Core;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Marimo.Kotowari.Tests.Core;

// 継承元ごとのTryParseCoreを呼ぶことで、外部派生型のフォールバックを観測する。
internal sealed class UpperCaseOrParser() : OrParser<char>(new CharParser('a'), new CharParser('b'))
{
    public int 呼び出し回数 { get; private set; }

    protected override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        呼び出し回数++;
        if (!base.TryParseCore(start, out next, out parsed))
        {
            return false;
        }

        parsed = char.ToUpper(parsed);
        return true;
    }
}
