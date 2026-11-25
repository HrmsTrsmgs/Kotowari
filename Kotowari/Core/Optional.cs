namespace Marimo.Kotowari.Core;

/// <summary>値の有無を表します。存在する値が null の場合と不在の場合は区別されます。</summary>
public readonly record struct Optional<T>
{
    /// <summary>値が存在するかどうか。値そのものの null 判定とは独立です。</summary>
    public bool IsPresent { get; }

    /// <summary>存在する場合はその値、不在の場合は型の既定値を返します。</summary>
    public T? Value { get; }

    /// <summary>存在の有無と値を組み合わせます。</summary>
    /// <param name="isPresent">値が存在する場合は true。</param>
    /// <param name="value">存在する場合の値。不在の場合、この引数は保持しません。</param>
    public Optional(bool isPresent, T? value)
    {
        IsPresent = isPresent;
        Value = isPresent ? value : default;
    }
}
