using Marimo.Kotowari.Core;
using System;

namespace Marimo.Kotowari;

/// <summary>解析の成否と解析後のカーソルを表します。</summary>
/// <typeparam name="T">成功時の値の型。nullを許容する型では成功値もnullを保持できます。</typeparam>
public abstract class ParseResult<T>
{
    /// <summary>解析が成功したかどうかを取得します。</summary>
    public abstract bool IsSuccess { get; }

    /// <summary>解析後のカーソルを取得します。</summary>
    public Cursol Cursol { get; }

    /// <summary>成功時の解析値を取得します。</summary>
    /// <exception cref="InvalidOperationException">解析が失敗した場合にスローされます。</exception>
    public abstract T Parsed { get; }

    internal ParseResult(Cursol cursol)
        => Cursol = cursol;

    /// <summary>指定したカーソルと値を保持する成功結果を作成します。</summary>
    /// <param name="cursol">解析後のカーソル。</param>
    /// <param name="parsed">成功値。null許容性は<typeparamref name="T"/>に従います。</param>
    /// <returns>成功結果。</returns>
    public static ParseResult<T> Success(Cursol cursol, T parsed)
        => new SuccessfulParseResult<T>(cursol, parsed);

    /// <summary>値を保持しない失敗結果を作成します。</summary>
    /// <param name="cursol">解析後のカーソル。</param>
    /// <returns>失敗結果。</returns>
    public static ParseResult<T> Failure(Cursol cursol)
        => new FailedParseResult<T>(cursol);
}
