using Marimo.Kotowari.Core;

namespace Marimo.Kotowari;

/// <summary>成功時だけ解析値を保持します。</summary>
internal sealed class SuccessfulParseResult<T>(Cursol cursol, T parsed) : ParseResult<T>(cursol)
{
    public override bool IsSuccess => true;

    public override T Parsed { get; } = parsed;
}
