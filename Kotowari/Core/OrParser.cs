using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Core;

/// <summary>候補を指定順に同じ開始位置から試し、最初の成功を返します。全候補が失敗すれば開始位置で失敗します。</summary>
/// <param name="parsers">順に試す候補。渡した配列の要素の差し替えは、その後の解析にも反映されます。</param>
public class OrParser<T>(params Parser<T>[] parsers) : Parser<T>
{
    Parser<T>[] Parsers { get; } = parsers;

    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
    {
        foreach (var parser in Parsers)
        {
            if (parser.TryParseCore(start, out next, out parsed))
            {
                return true;
            }
        }
        next = start;
        parsed = default;
        return false;
    }
}
