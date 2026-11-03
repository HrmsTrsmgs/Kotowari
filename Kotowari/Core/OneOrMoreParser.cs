using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class OneOrMoreParser<T> : Parser<IEnumerable<T>>
    where T : notnull
{
    ZeroOrMoreParser<T> Parser { get; }

    public OneOrMoreParser(Parser<T> parser)
        => Parser = new(parser);

    /// <summary>1つ以上の要素を繰り返し解析します。</summary>
    /// <exception cref="InvalidOperationException">要素パーサーが入力位置を進めずに成功した場合にスローされます。</exception>
    protected internal override ParseResult<IEnumerable<T>> ParseCore(Cursol cursol)
        => Parser.ParseCore(cursol, requireFirst: true);
}
