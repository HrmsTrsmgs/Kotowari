using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http.Headers;
using System.Transactions;

namespace Marimo.Kotowari.Core;

public class WordParser : Parser<string>
{
    Parser<char>[] Parsers { get; }
    public bool IgnoreCase { get; }

    public WordParser(string word, bool ignoreCase = false)
        : this(word, ignoreCase, new CharParser(' '))
    {
    }

    public WordParser(string word, Parser<char> whiteSpace)
        : this(word, false, whiteSpace)
    {
    }

    public WordParser(string word, bool ignoreCase, Parser<char> whiteSpace)
    {
        Parsers = [.. word.Select(c => new CharParser(c, ignoreCase))];
        IgnoreCase = ignoreCase;

        ArgumentNullException.ThrowIfNull(whiteSpace);
    }

    /// <summary>単語を解析します。</summary>
    protected internal override ParseResult<string> ParseCore(Cursol cursol)
    {
        var current = cursol;
        // 単語長が既知なので、Listの拡張と最後の配列化を避ける。
        // 最初の成功まで確保を遅らせ、先頭失敗では割り当てない。
        // 短語の比較ではList・ジェネレーター・stackalloc版より速かった。変更時は再比較する。
        // 測定と採用理由: Kotowari.Benchmarks/Results/2026-10-05-word-parser-production-deferred-array.md
        char[]? returnValue = null;
        var index = 0;
        foreach (var parser in Parsers)
        {
            var result = parser.ParseCore(current);
            if (!result.IsSuccess) return ParseResult<string>.Failure(cursol);

            current = result.Cursol;
            returnValue ??= new char[Parsers.Length];
            returnValue[index++] = result.Parsed;
        }

        return ParseResult<string>.Success(current,
            returnValue is null ? string.Empty : new string(returnValue));
    }
}
