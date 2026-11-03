using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Marimo.Kotowari;

public class WithWhiteSpaceParser<T> : Parser<T>
    where T : notnull
{
    Parser<char> WhiteSpace { get; }
    Parser<T> Parser { get; }
    public WithWhiteSpaceParser(Parser<char> whiteSpace, Parser<T> parser)
    {
        WhiteSpace = whiteSpace;
        Parser = parser;
    }

    /// <summary>前置空白を読み飛ばして値を解析します。</summary>
    /// <exception cref="InvalidOperationException">空白パーサーが入力位置を進めずに成功した場合にスローされます。</exception>
    protected internal override ParseResult<T> ParseCore(Cursol cursol)
    {
        var current = cursol;
        while (true)
        {
            var result = WhiteSpace.ParseCore(current);
            if (!result.IsSuccess)
            {
                break;
            }

            if (result.Cursol.Index == current.Index)
            {
                throw new InvalidOperationException("空白パーサーが入力を消費せず成功しました。");
            }

            current = result.Cursol;
        }

        return Parser.ParseCore(current);
    }
}
