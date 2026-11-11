using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Marimo.Kotowari;

public class WithWhiteSpaceParser<T> : Parser<T>
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
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
    {
        var current = start;
        while (true)
        {
            if (!WhiteSpace.TryParseCore(current, out var whiteSpaceNext, out _))
            {
                break;
            }

            if (whiteSpaceNext.Index == current.Index)
            {
                throw new InvalidOperationException("空白パーサーが入力を消費せず成功しました。");
            }

            current = whiteSpaceNext;
        }

        return Parser.TryParseCore(current, out next, out parsed);
    }
}
