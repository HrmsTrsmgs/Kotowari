using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Core;

public readonly record struct Cursol
{
    public const char Null = '\0';

    readonly char[] text;

    public char Current
    {
        get
        {

            try
            {
                return text[Index];
            }
            catch (IndexOutOfRangeException)
            {
                return Null;
            }
        }
    }

    public int Index { get; private init; }
    public ReadOnlySpan<char> Text => text;
    public Cursol(string text) : this(text, 0)
    {}
    private Cursol(string text, int index)
    {
        this.text = text.ToCharArray();
        Index = index;
    }

    public Cursol GoFoward(int step)
        => this with { Index = Math.Min(Index + step, text.Length) };

    public Cursol Copy() => this with { };
}
