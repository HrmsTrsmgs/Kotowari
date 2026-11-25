using System;

namespace Marimo.Kotowari.Core;

/// <summary>条件を満たす現在位置の文字を返し、満たさない場合は開始位置で失敗します。</summary>
/// <param name="condition">現在位置の文字を受け取り、受理するかを判定する関数。</param>
public class ConditionalCharParser(Func<char, bool> condition) : Parser<char>
{
    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        var c = start.Current;
        if (condition(c))
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
