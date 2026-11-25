using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Marimo.Kotowari.Core;

/// <summary>指定した単語を順に照合し、入力に現れた表記を返します。途中で不一致になれば開始位置で失敗します。</summary>
public class WordParser : Parser<string>
{
    Parser<char>[] Parsers { get; }

    /// <summary>文字の照合で大文字と小文字を区別しないかどうか。</summary>
    public bool IgnoreCase { get; }

    /// <summary>前後の空白を読み飛ばさずに単語を照合します。</summary>
    /// <param name="word">照合する単語。空の文字列は入力を消費せず成功します。</param>
    /// <param name="ignoreCase">解析時のカルチャーで大文字と小文字を区別せずに照合する場合は true。</param>
    public WordParser(string word, bool ignoreCase = false)
        : this(word, ignoreCase, new CharParser(' '))
    {
    }

    /// <summary>前後の空白を読み飛ばさず、大文字と小文字を区別して照合します。</summary>
    /// <param name="word">照合する単語。</param>
    /// <param name="whiteSpace">null は指定できません。この引数で前後の空白を消費することはありません。</param>
    public WordParser(string word, Parser<char> whiteSpace)
        : this(word, false, whiteSpace)
    {
    }

    /// <summary>空白パーサーの指定とは独立に、単語と大小文字の指定で照合します。</summary>
    /// <param name="word">照合する単語。</param>
    /// <param name="ignoreCase">解析時のカルチャーで大文字と小文字を区別せずに照合する場合は true。</param>
    /// <param name="whiteSpace">null は指定できません。後置空白を消費するには WithWhiteSpace を使用します。</param>
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
