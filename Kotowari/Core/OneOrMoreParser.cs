using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Core;

/// <summary>一つ以上の成功値を順に集めます。最初の子が失敗すれば開始位置で失敗し、後続の失敗ではその位置までの結果を返します。</summary>
/// <param name="parser">反復するパーサー。成功した場合は入力位置を変える必要があります。</param>
public class OneOrMoreParser<T>(Parser<T> parser) : Parser<IEnumerable<T>>
{
    ZeroOrMoreParser<T> Parser { get; } = new(parser);

    /// <summary>1つ以上の要素を繰り返し解析します。</summary>
    /// <exception cref="InvalidOperationException">要素パーサーが入力位置を進めずに成功した場合にスローされます。</exception>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out IEnumerable<T> parsed)
        => Parser.TryParseCore(start, out next, out parsed, requireFirst: true);
}
