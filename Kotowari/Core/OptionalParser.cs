using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

/// <summary>
/// 子が成功したら、その値と消費位置を保って存在値で成功します。
/// 子が失敗したら、入力消費の有無にかかわらず呼び出し開始位置へ戻り、不在の値で成功します。
/// 子が送出した例外はそのまま伝播します。
/// </summary>
public class OptionalParser<T> : Parser<Optional<T>>
    where T : notnull
{
    Parser<T> Parser { get; }
    public OptionalParser(Parser<T> parser)
        => Parser = parser;

    protected internal override ParseResult<Optional<T>> ParseCore(Cursol cursol)
    {
        var result = Parser.ParseCore(cursol);
        var parsed = result.IsSuccess ? result.Parsed : default;

        return ParseResult<Optional<T>>.Success(
            result.IsSuccess ? result.Cursol : cursol,
            new Optional<T>(result.IsSuccess, parsed));
    }
}
