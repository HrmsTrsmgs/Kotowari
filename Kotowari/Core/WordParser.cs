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

    Parser<char> WhiteSpace { get; }

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

        WhiteSpace = whiteSpace ?? throw new ArgumentNullException(nameof(whiteSpace));
    }

    protected internal override ParseResult<string, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
    {
        var current = SkipBlankAsync(cursol);
        // 単語長が既知なので、Listの拡張と最後の配列化を避ける。
        // 最初の成功まで確保を遅らせ、先頭失敗では割り当てない。
        // 短語の比較ではList・ジェネレーター・stackalloc版より速かった。変更時は再比較する。
        // 測定と採用理由: Kotowari.Benchmarks/Results/2026-10-05-word-parser-production-deferred-array.md
        char[]? returnValue = null;
        var index = 0;
        foreach (var parser in Parsers)
        {
            var result = parser.ParseCore(current);
            if (!result.IsSuccess) return ParseResult<string, TDocument>.Failure(cursol);

            current = result.Cursol;
            returnValue ??= new char[Parsers.Length];
            returnValue[index++] = result.Parsed;
        }

        current = SkipBlankAsync(current);
        return ParseResult<string, TDocument>.Success(current,
            returnValue is null ? string.Empty : new string(returnValue));
    }

    Cursol<TDocument> SkipBlankAsync<TDocument>(Cursol<TDocument> current)
        where TDocument : IDocument, allows ref struct
    {
        while (true)
        {
            var result = WhiteSpace.ParseCore(current);
            if (!result.IsSuccess) return current;

            current = result.Cursol;
        }
    }
}
