using Marimo.Kotowari.Core;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Marimo.Kotowari.Tests.Core;

// 継承元ごとのParseCoreを呼ぶことで、外部派生型のフォールバックを観測する。
internal sealed class IncrementingParserConverter() : ParserConverter<char, int>(new CharParser('a'), it => it)
{
    public int 呼び出し回数 { get; private set; }

    protected override ParseResult<int, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        呼び出し回数++;
        return TestParseResults.ConvertSuccessfulValue(base.ParseCore(cursol), static it => it + 1);
    }
}
