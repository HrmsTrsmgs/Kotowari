using Marimo.Kotowari.Core;
using System;

namespace Marimo.Kotowari;

public readonly struct ParseResult<T>
    where T : notnull
{
    readonly T? parsed;

    public bool IsSuccess { get; }

    public Cursol Cursol { get; }

    public T Parsed
        => IsSuccess && parsed is not null
            ? parsed
            : throw new InvalidOperationException();

    ParseResult(bool isSuccess, Cursol cursol, T? parsed)
    {
        IsSuccess = isSuccess;
        Cursol = cursol;
        this.parsed = parsed;
    }

    public static ParseResult<T> Success(Cursol cursol, T parsed)
    {
        if (parsed is null)
        {
            throw new ArgumentNullException(nameof(parsed));
        }

        return new(true, cursol, parsed);
    }

    public static ParseResult<T> Failure(Cursol cursol)
        => new(false, cursol, default);
}
