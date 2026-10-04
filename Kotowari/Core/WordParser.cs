using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Transactions;

namespace Marimo.Kotowari.Core;

public class WordParser : Parser<string>
{
    readonly string word;
    readonly bool ignoreCase;
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
        // 既存のLINQ生成と同じ引数名・検証順を維持する。
        this.word = word ?? throw new ArgumentNullException("source");
        this.ignoreCase = ignoreCase;

        WhiteSpace = whiteSpace ?? throw new ArgumentNullException(nameof(whiteSpace));
    }

    protected override ParseResult<string> ParseCore(Cursol cursol)
    {
        var current = SkipBlankAsync(cursol);
        var start = current;

        // 範囲外のCurrentは終端文字を返すため、入力長だけでは失敗と判断できない。
        char[]? captured = null;
        if (word.Length > 0 && (start.Index < 0 || word.Length > start.Text.Length - start.Index))
        {
            captured = new char[word.Length];
        }

        for (var index = 0; index < word.Length; index++)
        {
            var currentChar = current.Current;
            var expectedChar = word[index];
            if (ignoreCase
                ? char.ToLower(currentChar) != char.ToLower(expectedChar)
                : currentChar != expectedChar)
            {
                return ParseResult<string>.Failure(cursol);
            }

            captured?[index] = currentChar;

            current = current.GoFoward(1);
        }

        // 末尾の空白パーサーが文書を書き換える前に解析値を確定する。
        var parsed = word.Length == 0
            ? string.Empty
            : captured is null
                ? new string(start.Text, start.Index, word.Length)
            : new string(captured);
        current = SkipBlankAsync(current);
        return ParseResult<string>.Success(current, parsed);
    }

    private Cursol SkipBlankAsync(Cursol current)
    {
        while (true)
        {
            var result = WhiteSpace.Parse(current);
            if (!result.IsSuccess) return current;

            current = result.Cursol;
        }
    }
}
