using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core
{
    public class ParserConverter<U, T> : Parser<T>
    {
        Parser<U> Parser { get; }
        Func<U, T> Converter { get; }

        public ParserConverter(Parser<U> parser, Func<U, T> converter)
        {
            Parser = parser;
            Converter = converter;
        }

        protected override (bool isSuccess, Cursol cursol, T parsed) ParseCore(Cursol cursol)
        {
            var (isSuccess, afterCursol, parsed) = Parser.Parse(cursol);
            
            return (isSuccess, afterCursol, (isSuccess ? Converter(parsed) : default));
        }
    }
}
