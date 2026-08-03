using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core
{
    public class CharParser : Parser<char>
    {
        char Char { get; }

        bool IgnoreCase { get; }

        public CharParser(char @char, bool ignoreCase = false)
        {
            Char = @char;
            IgnoreCase = ignoreCase;
        }

        protected override (bool isSuccess, Cursol cursol, char parsed) ParseCore(Cursol cursol)
            => cursol.Current switch
            {
                var c when (IgnoreCase  ? char.ToLower(c) == char.ToLower(Char) : c == Char)
                        => (true, cursol.GoFoward(1), c),
                _ => (false, cursol, default)
            };
    }
}
