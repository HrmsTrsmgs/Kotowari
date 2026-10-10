using Marimo.Kotowari.Core;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Marimo.Kotowari.Tests.Core;

// 継承元ごとのParseCoreを呼ぶことで、外部派生型のフォールバックを観測する。
internal sealed class UpperCaseZeroOrMoreParser() : ZeroOrMoreParser<char>(new CharParser('a'))
{
    public int 呼び出し回数 { get; private set; }

    protected override ParseResult<IEnumerable<char>, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        呼び出し回数++;
        var result = base.ParseCore(cursol);
        return ParseResult<IEnumerable<char>, TDocument>.Success(
            result.Cursol,
            result.Parsed.Select(char.ToUpper));
    }
}
