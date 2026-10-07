using Marimo.Kotowari.Core;
using System;

namespace Marimo.Kotowari;

/// <summary>
/// 同期解析の成否、読み取り位置、および成功時の値を保持します。
/// </summary>
/// <remarks>
/// カーソルが入力を借用するため、ヒープへの格納、boxing、ラムダでの捕捉はできません。
/// 結果はカーソルが借用した入力の有効期間内で使用してください。
/// </remarks>
public readonly ref struct ParseResult<T, TDocument>
    where T : notnull
    where TDocument : IDocument, allows ref struct
{
    readonly T? parsed;

    public bool IsSuccess { get; }

    public Cursol<TDocument> Cursol { get; }

    public T Parsed
        => IsSuccess && parsed is not null
            ? parsed
            : throw new InvalidOperationException();

    ParseResult(bool isSuccess, Cursol<TDocument> cursol, T? parsed)
    {
        IsSuccess = isSuccess;
        Cursol = cursol;
        this.parsed = parsed;
    }

    public static ParseResult<T, TDocument> Success(Cursol<TDocument> cursol, T parsed)
    {
        if (parsed is null)
        {
            throw new ArgumentNullException(nameof(parsed));
        }

        return new(true, cursol, parsed);
    }

    public static ParseResult<T, TDocument> Failure(Cursol<TDocument> cursol)
        => new(false, cursol, default);
}
