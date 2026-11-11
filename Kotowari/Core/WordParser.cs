using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out string parsed)
    {
        var current = start;
        // 単語長が既知なので、Listの拡張と最後の配列化を避ける。
        // 最初の成功まで確保を遅らせ、先頭失敗では割り当てない。
        // 短語の比較ではList・ジェネレーター・stackalloc版より速かった。変更時は再比較する。
        // 測定と採用理由: Kotowari.Benchmarks/Results/2026-10-05-word-parser-production-deferred-array.md
        char[]? returnValue = null;
        var index = 0;
        foreach (var parser in Parsers)
        {
            if (!parser.TryParseCore(current, out var childNext, out var childParsed))
            {
                next = start;
                parsed = default;
                return false;
            }

            current = childNext;
            returnValue ??= new char[Parsers.Length];
            returnValue[index++] = childParsed;
        }

        next = current;
        parsed = returnValue is null ? string.Empty : new string(returnValue);
        return true;
    }
}
