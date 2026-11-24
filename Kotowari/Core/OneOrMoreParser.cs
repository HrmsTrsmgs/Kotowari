using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OneOrMoreParser<T> : Parser<IEnumerable<T>>
{
    ZeroOrMoreParser<T> Parser { get; }

    public OneOrMoreParser(Parser<T> parser)
        => Parser = new(parser);

    /// <summary>1つ以上の要素を繰り返し解析します。</summary>
    /// <exception cref="InvalidOperationException">要素パーサーが入力位置を進めずに成功した場合にスローされます。</exception>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out IEnumerable<T> parsed)
        => Parser.TryParseCore(start, out next, out parsed, requireFirst: true);
}
