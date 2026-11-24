using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin;
using Sprache;
using Superpower;
using Superpower.Parsers;

namespace Marimo.Kotowari.Benchmarks;


internal static class PidginJsonParser
{
    private static readonly Parser<char, Unit> WhiteSpaces = Pidgin.Parser.SkipWhitespaces;
    private static readonly Parser<char, char> Quote = Pidgin.Parser.Char('"');
    private static readonly Parser<char, string> String = Pidgin.Parser<char>.Token(c => c != '"' && c != '\\')
        .ManyString()
        .Between(Quote);
    private static readonly Parser<char, string> Number = Pidgin.Parser<char>.Token(c => char.IsDigit(c) || c is '-' or '+' or '.' or 'e' or 'E')
        .AtLeastOnceString();

    private static readonly Parser<char, BenchmarkJson> JsonString = String.Select<BenchmarkJson>(x => new BenchmarkJsonString(x));
    private static readonly Parser<char, BenchmarkJson> JsonNumber = Number.Select<BenchmarkJson>(x => new BenchmarkJsonNumber(x));
    private static readonly Parser<char, BenchmarkJson> JsonBoolean = Pidgin.Parser.String("true").ThenReturn<BenchmarkJson>(new BenchmarkJsonBoolean(true))
        .Or(Pidgin.Parser.String("false").ThenReturn<BenchmarkJson>(new BenchmarkJsonBoolean(false)));
    private static readonly Parser<char, BenchmarkJson> JsonNull = Pidgin.Parser.String("null").ThenReturn<BenchmarkJson>(new BenchmarkJsonNull());
    private static readonly Parser<char, BenchmarkJson> Json = JsonString.Or(JsonBoolean).Or(JsonNull).Or(JsonNumber)
        .Or(Pidgin.Parser.Rec(
            () => JsonArray ?? throw new InvalidOperationException()))
        .Or(Pidgin.Parser.Rec(
            () => JsonObject ?? throw new InvalidOperationException()));
    private static readonly Parser<char, BenchmarkJson> Value = Json.Between(WhiteSpaces);
    private static readonly Parser<char, BenchmarkJson> JsonArray = Value.Separated(Pidgin.Parser.Char(','))
        .Between(Pidgin.Parser.Char('['), Pidgin.Parser.Char(']'))
        .Select<BenchmarkJson>(xs => new BenchmarkJsonArray(xs.ToArray()));
    private static readonly Parser<char, KeyValuePair<string, BenchmarkJson>> Member = String.Between(WhiteSpaces)
        .Before(Pidgin.Parser.Char(':'))
        .Then(Value, static (key, value) => new KeyValuePair<string, BenchmarkJson>(key, value));
    private static readonly Parser<char, BenchmarkJson> JsonObject = Member.Separated(Pidgin.Parser.Char(','))
        .Between(Pidgin.Parser.Char('{'), Pidgin.Parser.Char('}'))
        .Select<BenchmarkJson>(xs => new BenchmarkJsonObject(new Dictionary<string, BenchmarkJson>(xs)));
    private static readonly Parser<char, BenchmarkJson> Document = Value.Before(Pidgin.Parser<char>.End);

    public static BenchmarkJson Parse(string input) => Document.ParseOrThrow(input);
}
