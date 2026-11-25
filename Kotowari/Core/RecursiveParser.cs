using System;
using System.Diagnostics.CodeAnalysis;

namespace Marimo.Kotowari.Core;

/// <summary>解析がこの位置に到達したときに委譲先を取得するため、構築中の文法も再帰的に参照できます。</summary>
/// <param name="parserGetter">構築時ではなく、解析の呼び出しごとに使用するパーサーを返す関数。</param>
public class RecursiveParser<T>(Func<Parser<T>> parserGetter) : Parser<T>
{
    /// <inheritdoc/>
    protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
        => parserGetter().TryParseCore(start, out next, out parsed);
}
