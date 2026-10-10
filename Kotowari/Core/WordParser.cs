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
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<string, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(WordParser)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    sealed class Execution<TDocument> : ParserExecution<string, TDocument>
        where TDocument : IDocument, allows ref struct
    {
        readonly ParserExecution<char, TDocument>[] parsers;
        readonly ParserExecution<char, TDocument> whiteSpace;

        Execution(WordParser definition) : base(definition)
        {
            parsers = [.. definition.Parsers.Select(it => it.CreateExecution<TDocument>())];
            whiteSpace = definition.WhiteSpace.CreateExecution<TDocument>();
        }

        internal static Execution<TDocument> Get(WordParser definition)
            => ParserExecutionCache<WordParser, Execution<TDocument>>.Get(
                definition, static it => new Execution<TDocument>(it));

        internal override ParseResult<string, TDocument> ParseCore(Cursol<TDocument> cursol)
        {
            var current = WhiteSpaceExecution.Skip(whiteSpace, cursol);
            // 単語長が既知なので、Listの拡張と最後の配列化を避ける。
            // 最初の成功まで確保を遅らせ、先頭失敗では割り当てない。
            // 短語の比較ではList・ジェネレーター・stackalloc版より速かった。変更時は再比較する。
            // 測定と採用理由: Kotowari.Benchmarks/Results/2026-10-05-word-parser-production-deferred-array.md
            char[]? returnValue = null;
            var index = 0;
            foreach (var parser in parsers)
            {
                var result = parser.ParseCore(current);
                if (!result.IsSuccess) return ParseResult<string, TDocument>.Failure(cursol);

                current = result.Cursol;
                returnValue ??= new char[parsers.Length];
                returnValue[index++] = result.Parsed;
            }

            current = WhiteSpaceExecution.Skip(whiteSpace, current);
            return ParseResult<string, TDocument>.Success(current,
                returnValue is null ? string.Empty : new string(returnValue));
        }
    }
}
