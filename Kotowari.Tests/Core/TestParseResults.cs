using Marimo.Kotowari.Core;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Marimo.Kotowari.Tests.Core;

// 継承元ごとのParseCoreを呼ぶことで、外部派生型のフォールバックを観測する。
internal static class TestParseResults
{
    internal static ParseResult<T> ConvertSuccessfulValue<T>(
        ParseResult<T> result, Func<T, T> convert)
        where T : notnull
        => result.IsSuccess
            ? ParseResult<T>.Success(result.Cursol, convert(result.Parsed))
            : result;
}
