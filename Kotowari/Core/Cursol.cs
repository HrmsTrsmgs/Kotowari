using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Kotowari.Core
{
    public struct Cursol
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

        public int Index { get; }
        public char[] Text { get; }
        public Cursol(string text) : this(text, 0)
        {}
        private Cursol(string text, int index)
        {
            Text = text.ToCharArray();
            Index = index;
        }

        private Cursol(char[] text, int index)
        {
            Text = text;
            Index = index;
        }

        public Cursol GoFoward(int step) => new Cursol(Text, Math.Min(Index + step, Text.Length));

        public Cursol Copy() => new Cursol(Text, Index);
    }
}
