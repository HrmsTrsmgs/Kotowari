using Marimo.Kotowari.Core;
using System;

namespace Marimo.Kotowari;

/// <summary>失敗時はカーソルだけを保持し、解析値を持ちません。</summary>
internal sealed class FailedParseResult<T>(Cursol cursol) : ParseResult<T>(cursol)
{
    public override bool IsSuccess => false;

    public override T Parsed => throw new InvalidOperationException();
}
