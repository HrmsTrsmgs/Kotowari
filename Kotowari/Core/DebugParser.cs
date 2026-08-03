using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core
{
    public class DebugParser<T> : Parser<T>
    {
        Parser<T> Parser { get; }
        Action HasBreakPoint { get; }
        public DebugParser(Parser<T> parser, Action hasBreakPoint)
        {
            Parser = parser;
            HasBreakPoint = hasBreakPoint;
        }
        protected override (bool isSuccess, Cursol cursol, T parsed) ParseCore(Cursol cursol)
        {
            HasBreakPoint();
            return Parser.Parse(cursol);
        }
    }
}
