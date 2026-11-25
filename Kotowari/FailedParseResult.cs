using Marimo.Kotowari.Core;
using System;

namespace Marimo.Kotowari;

/// <summary>失敗時はカーソルだけを保持し、解析値を持ちません。</summary>
internal sealed class FailedParseResult<T>(Cursol cursol) : ParseResult<T>(cursol)
{
    /// <inheritdoc/>
    public override bool IsSuccess => false;

    /// <inheritdoc/>
    public override T Parsed => throw new InvalidOperationException();
}
