using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Core;

public readonly record struct Cursol
{
    public static char Null = '\0';

    public ref char Current
    {
        get
        {

            try
            {
                return ref Text[Index];
            }
            catch (IndexOutOfRangeException)
            {
                return ref Null;
            }
        }
    }

    public int Index { get; private init; }
    public char[] Text { get; }
    public Cursol(string text) : this(text, 0)
    {}
    private Cursol(string text, int index)
    {
        Text = text.ToCharArray();
        Index = index;
    }

    public Cursol GoFoward(int step)
        => this with { Index = Math.Min(Index + step, Text.Length) };

    public Cursol Copy() => this with { };
}
