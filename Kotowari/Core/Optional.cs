using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari.Core;

public readonly record struct Optional<T>
    where T : notnull
{
    public bool IsPresent { get; }

    public T? Value { get; }

    public Optional(bool isPresent, T? value)
    {
        IsPresent = isPresent;
        Value = isPresent
            ? value ?? throw new ArgumentNullException(nameof(value))
            : default;
    }
}
