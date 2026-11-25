namespace Marimo.Kotowari.Core;

/// <summary>指定のパーサーが失敗したときだけ、開始位置の文字を読みます。判定用パーサーの消費位置は引き継ぎません。</summary>
/// <param name="expectChars">受理したくない文字を識別するパーサー。</param>
public class ExpectCharParser(Parser<char> expectChars) : Parser<char>
{
    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        if (expectChars.TryParseCore(start, out _, out _))
        {
            next = start;
            parsed = default;
            return false;
        }

        next = start.GoFoward(1);
        parsed = start.Current;
        return true;
    }
}
