namespace Marimo.Kotowari.Core;

/// <summary>現在位置の文字が指定文字に一致したとき、入力側の文字を返します。不一致では入力を消費しません。</summary>
/// <param name="char">照合対象の文字。</param>
/// <param name="ignoreCase">true の場合は解析時のカルチャーで小文字化して比較します。</param>
public class CharParser(char @char, bool ignoreCase = false) : Parser<char>
{
    char Char { get; } = @char;
    bool IgnoreCase { get; } = ignoreCase;

    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        var c = start.Current;
        if (IgnoreCase ? char.ToLower(c) == char.ToLower(Char) : c == Char)
        {
            next = start.GoFoward(1);
            parsed = c;
            return true;
        }

        next = start;
        parsed = default;
        return false;
    }
}
