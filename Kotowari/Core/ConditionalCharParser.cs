using System;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Kotowari.Core
{
    public class ConditionalCharParser : Parser<char>
    {
        Func<char, bool> Condition { get; }

        public ConditionalCharParser(Func<char, bool> condition)
        {
            Condition = condition;
        }

        protected override (bool isSuccess, Cursol cursol, char parsed) ParseCore(Cursol cursol)
            => cursol.Current switch
            {
                var c when (Condition(c))
                        => (true, cursol.GoFoward(1), c),
                _ => (false, cursol, default)
            };
    }
}
