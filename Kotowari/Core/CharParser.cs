using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class CharParser : Parser<char>
{
    char Char { get; }

    bool IgnoreCase { get; }

    readonly byte[]? utf8;

    public CharParser(char @char, bool ignoreCase = false)
    {
        Char = @char;
        IgnoreCase = ignoreCase;
        // 文法定義時だけ符号化する。単独サロゲートは置換文字に一致させず、通常処理へ戻す。
        utf8 = char.IsSurrogate(@char) ? null : Encoding.UTF8.GetBytes(@char.ToString());
    }

    protected internal override ParseResult<char, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => ParseCharacter(cursol);

    internal override ParserExecution<char, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(CharParser)
            ? new Execution<TDocument>(this)
            : base.CreateExecution<TDocument>();

    ParseResult<char, TDocument> ParseCharacter<TDocument>(Cursol<TDocument> cursol)
        where TDocument : IDocument, allows ref struct
    {
        if (utf8 is not null && cursol.TryGetUtf8(out var bytes) && !bytes.IsEmpty)
        {
            if (bytes.StartsWith(utf8))
                return ParseResult<char, TDocument>.Success(cursol.AdvanceUtf8Bytes(utf8.Length), Char);

            if (bytes[0] < 0x80)
            {
                var current = (char)bytes[0];
                return IgnoreCase && char.ToLower(current) == char.ToLower(Char)
                    ? ParseResult<char, TDocument>.Success(cursol.AdvanceUtf8Bytes(1), current)
                    : ParseResult<char, TDocument>.Failure(cursol);
            }
        }

        // 非ASCIIの大小文字比較、終端のNUL、補助平面文字の拒否は文字読み取りの契約を使う。
        return cursol.Current switch
        {
            var c when (IgnoreCase ? char.ToLower(c) == char.ToLower(Char) : c == Char)
                    => ParseResult<char, TDocument>.Success(cursol.GoFoward(1), c),
            _ => ParseResult<char, TDocument>.Failure(cursol)
        };
    }

    sealed class Execution<TDocument>(CharParser parser) : ParserExecution<char, TDocument>(parser)
        where TDocument : IDocument, allows ref struct
    {
        internal override ParseResult<char, TDocument> ParseCore(Cursol<TDocument> cursol)
            => parser.ParseCharacter(cursol);
    }
}
