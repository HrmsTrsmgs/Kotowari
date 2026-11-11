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
/// <param name="parser">値の有無を判定する子パーサー。</param>
public class OptionalParser<T>(Parser<T> parser) : Parser<Optional<T>>
{
    Parser<T> Parser { get; } = parser;

    protected internal override bool TryParseCore(Cursol start, out Cursol next, out Optional<T> parsed)
    {
        var isSuccess = Parser.TryParseCore(start, out next, out var childParsed);
        if (!isSuccess)
        {
            next = start;
        }

        parsed = new Optional<T>(isSuccess, childParsed);
        return true;
    }
}
