using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Transactions;
using System.Data.SqlTypes;

namespace Marimo.Kotowari.Core;

public class SequenceParser<T1, T2> : Parser<(T1, T2)>
{
    (Parser<T1>, Parser<T2>) Parsers { get; }
    public SequenceParser(Parser<T1> parser1, Parser<T2> parser2)
        => Parsers = (parser1, parser2);
    protected override (bool isSuccess, Cursol cursol, (T1, T2) parsed) ParseCore(Cursol cursol)
    {
        var (isSuccess, current, parsed1) = Parsers.Item1.Parse(cursol);
        if (!isSuccess)
        {
            return (false, cursol, default);
        }
        T2 parsed2;
        (isSuccess, current, parsed2) = Parsers.Item2.Parse(current);
        if (!isSuccess)
        {
            return (false, cursol, default);
        }
        return (true, current, (parsed1, parsed2));
    }
}
public class SequenceParser<T1, T2, T3> : Parser<(T1, T2, T3)>
{
    Parser<(T1, T2, T3)> Parser { get; }
    public SequenceParser(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3)), (T1, T2, T3)>(
            new SequenceParser<T1, (T2, T3)>(
                parser1,
                new SequenceParser<T2, T3>(
                    parser2,
                    parser3)),
            tuple => (tuple.Item1, tuple.Item2.Item1, tuple.Item2.Item2));
    }
    protected override (bool isSuccess, Cursol cursol, (T1, T2, T3) parsed) ParseCore(Cursol cursol)
        => Parser.Parse(cursol);
}
public class SequenceParser<T1, T2, T3, T4> : Parser<(T1, T2, T3, T4)>
{
    Parser<(T1, T2, T3, T4)> Parser { get; }
    public SequenceParser(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<T4> parser4)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3, T4)), (T1, T2, T3, T4)>(
            new SequenceParser<T1, (T2, T3, T4)>(
                parser1,
                new SequenceParser<T2, T3, T4>(
                    parser2,
                    parser3,
                    parser4)),
            tuple => (tuple.Item1, tuple.Item2.Item1, tuple.Item2.Item2, tuple.Item2.Item3));
    }

    protected override (bool isSuccess, Cursol cursol, (T1, T2, T3, T4) parsed) ParseCore(Cursol cursol)
        => Parser.Parse(cursol);
}

public class SequenceParser<T1, T2, T3, T4, T5> : Parser<(T1, T2, T3, T4, T5)>
{
    Parser<(T1, T2, T3, T4, T5)> Parser { get; }
    public SequenceParser(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<T4> parser4,
        Parser<T5> parser5)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3, T4, T5)), (T1, T2, T3, T4, T5)>(
            new SequenceParser<T1, (T2, T3, T4, T5)>(
                parser1,
                new SequenceParser<T2, T3, T4, T5>(
                    parser2,
                    parser3,
                    parser4,
                    parser5)),
            tuple => (
                tuple.Item1,
                tuple.Item2.Item1,
                tuple.Item2.Item2,
                tuple.Item2.Item3,
                tuple.Item2.Item4));
    }

    protected override (bool isSuccess, Cursol cursol, (T1, T2, T3, T4, T5) parsed) ParseCore(Cursol cursol)
        => Parser.Parse(cursol);
}

public class SequenceParser<T1, T2, T3, T4, T5, T6> : Parser<(T1, T2, T3, T4, T5, T6)>
{
    Parser<(T1, T2, T3, T4, T5, T6)> Parser { get; }
    public SequenceParser(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<T4> parser4,
        Parser<T5> parser5,
        Parser<T6> parser6)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3, T4, T5, T6)), (T1, T2, T3, T4, T5, T6)>(
            new SequenceParser<T1, (T2, T3, T4, T5, T6)>(
                parser1,
                new SequenceParser<T2, T3, T4, T5, T6>(
                    parser2,
                    parser3,
                    parser4,
                    parser5,
                    parser6)),
            tuple => (
                tuple.Item1,
                tuple.Item2.Item1,
                tuple.Item2.Item2,
                tuple.Item2.Item3,
                tuple.Item2.Item4,
                tuple.Item2.Item5));
    }

    protected override (bool isSuccess, Cursol cursol, (T1, T2, T3, T4, T5, T6) parsed) ParseCore(Cursol cursol)
        => Parser.Parse(cursol);
}

public class SequenceParser<T1, T2, T3, T4, T5, T6, T7> : Parser<(T1, T2, T3, T4, T5, T6, T7)>
{
    Parser<(T1, T2, T3, T4, T5, T6, T7)> Parser { get; }
    public SequenceParser(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<T4> parser4,
        Parser<T5> parser5,
        Parser<T6> parser6,
        Parser<T7> parser7)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3, T4, T5, T6, T7)), (T1, T2, T3, T4, T5, T6, T7)>(
            new SequenceParser<T1, (T2, T3, T4, T5, T6, T7)>(
                parser1,
                new SequenceParser<T2, T3, T4, T5, T6, T7>(
                    parser2,
                    parser3,
                    parser4,
                    parser5,
                    parser6,
                    parser7)),
            tuple => (
                tuple.Item1,
                tuple.Item2.Item1,
                tuple.Item2.Item2,
                tuple.Item2.Item3,
                tuple.Item2.Item4,
                tuple.Item2.Item5,
                tuple.Item2.Item6));
    }

    protected override (bool isSuccess, Cursol cursol, (T1, T2, T3, T4, T5, T6, T7) parsed) ParseCore(Cursol cursol)
        => Parser.Parse(cursol);
}
public class SequenceParser<T1, T2, T3, T4, T5, T6, T7, T8> : Parser<(T1, T2, T3, T4, T5, T6, T7, T8)>
{
    Parser<(T1, T2, T3, T4, T5, T6, T7, T8)> Parser { get; }
    public SequenceParser(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<T4> parser4,
        Parser<T5> parser5,
        Parser<T6> parser6,
        Parser<T7> parser7,
        Parser<T8> parser8)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3, T4, T5, T6, T7, T8)), (T1, T2, T3, T4, T5, T6, T7, T8)>(
            new SequenceParser<T1, (T2, T3, T4, T5, T6, T7, T8)>(
                parser1,
                new SequenceParser<T2, T3, T4, T5, T6, T7, T8>(
                    parser2,
                    parser3,
                    parser4,
                    parser5,
                    parser6,
                    parser7,
                    parser8)),
            tuple => (
                tuple.Item1,
                tuple.Item2.Item1,
                tuple.Item2.Item2,
                tuple.Item2.Item3,
                tuple.Item2.Item4,
                tuple.Item2.Item5,
                tuple.Item2.Item6,
                tuple.Item2.Item7));
    }

    protected override (bool isSuccess, Cursol cursol, (T1, T2, T3, T4, T5, T6, T7, T8) parsed) ParseCore(Cursol cursol)
        => Parser.Parse(cursol);
}
public class SequenceParser<T1, T2, T3, T4, T5, T6, T7, T8, T9> : Parser<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>
{
    Parser<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> Parser { get; }
    public SequenceParser(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<T4> parser4,
        Parser<T5> parser5,
        Parser<T6> parser6,
        Parser<T7> parser7,
        Parser<T8> parser8,
        Parser<T9> parser9)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3, T4, T5, T6, T7, T8, T9)), (T1, T2, T3, T4, T5, T6, T7, T8, T9)>(
            new SequenceParser<T1, (T2, T3, T4, T5, T6, T7, T8, T9)>(
                parser1,
                new SequenceParser<T2, T3, T4, T5, T6, T7, T8, T9>(
                    parser2,
                    parser3,
                    parser4,
                    parser5,
                    parser6,
                    parser7,
                    parser8,
                    parser9)),
            tuple => (
                tuple.Item1,
                tuple.Item2.Item1,
                tuple.Item2.Item2,
                tuple.Item2.Item3,
                tuple.Item2.Item4,
                tuple.Item2.Item5,
                tuple.Item2.Item6,
                tuple.Item2.Item7,
                tuple.Item2.Item8));
    }

    protected override (bool isSuccess, Cursol cursol, (T1, T2, T3, T4, T5, T6, T7, T8, T9) parsed) ParseCore(Cursol cursol)
        => Parser.Parse(cursol);
}
public class SequenceParser<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : Parser<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>
{
    Parser<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> Parser { get; }
    public SequenceParser(
        Parser<T1> parser1,
        Parser<T2> parser2,
        Parser<T3> parser3,
        Parser<T4> parser4,
        Parser<T5> parser5,
        Parser<T6> parser6,
        Parser<T7> parser7,
        Parser<T8> parser8,
        Parser<T9> parser9,
        Parser<T10> parser10)
    {
        Parser =
            new ParserConverter<(T1, (T2, T3, T4, T5, T6, T7, T8, T9, T10)), (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>(
            new SequenceParser<T1, (T2, T3, T4, T5, T6, T7, T8, T9, T10)>(
                parser1,
                new SequenceParser<T2, T3, T4, T5, T6, T7, T8, T9, T10>(
                    parser2,
                    parser3,
                    parser4,
                    parser5,
                    parser6,
                    parser7,
                    parser8,
                    parser9,
                    parser10)),
            tuple => (
                tuple.Item1,
                tuple.Item2.Item1,
                tuple.Item2.Item2,
                tuple.Item2.Item3,
                tuple.Item2.Item4,
                tuple.Item2.Item5,
                tuple.Item2.Item6,
                tuple.Item2.Item7,
                tuple.Item2.Item8,
                tuple.Item2.Item9));
    }

    protected override (bool isSuccess, Cursol cursol, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) parsed) ParseCore(Cursol cursol)
        => Parser.Parse(cursol);
}
