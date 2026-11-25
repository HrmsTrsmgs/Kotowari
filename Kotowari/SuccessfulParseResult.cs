using Marimo.Kotowari.Core;

namespace Marimo.Kotowari;

/// <summary>成功時だけ解析値を保持します。</summary>
internal sealed class SuccessfulParseResult<T>(Cursol cursol, T parsed) : ParseResult<T>(cursol)
{
    /// <inheritdoc/>
    public override bool IsSuccess => true;

    /// <inheritdoc/>
    public override T Parsed { get; } = parsed;
}
