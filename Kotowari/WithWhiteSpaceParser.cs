using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Marimo.Kotowari
{
    public class WithWhiteSpaceParser<T> : Parser<T>
    {
        Parser<char> WhiteSpace { get; }
        Parser<T> Parser { get; }
        public WithWhiteSpaceParser(Parser<char> whiteSpace, Parser<T> parser)
        {
            WhiteSpace = whiteSpace;
            Parser = parser;
        }

        protected override (bool isSuccess, Cursol cursol, T parsed) ParseCore(Cursol cursol)
        {
            var current = cursol;
            bool isSuccess;
            do
            {
                (isSuccess, current, _) = WhiteSpace.Parse(current);
            } while (isSuccess);

            return Parser.Parse(current);
        }
    }
}
