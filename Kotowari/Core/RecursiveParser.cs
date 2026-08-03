using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core
{
    public class RecursiveParser<T> : Parser<T>
    {
        Func<Parser<T>> ParserGetter { get; }
        public RecursiveParser(Func<Parser<T>> parserGetter)
        {
            ParserGetter = parserGetter;
        }

        protected override (bool isSuccess, Cursol cursol, T parsed) ParseCore(Cursol cursol)
            => ParserGetter().Parse(cursol);
    }
}
