using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin;

namespace Marimo.Kotowari.Benchmarks;

internal static class PidginJsonParser
{
    static readonly Parser<char, Unit> WhiteSpaces = Pidgin.Parser.SkipWhitespaces;
    static readonly Parser<char, char> Quote = Pidgin.Parser.Char('"');
    static readonly Parser<char, string> String = Pidgin.Parser<char>.Token(c => c != '"' && c != '\\')
        .ManyString()
        .Between(Quote);
    static readonly Parser<char, string> Number = Pidgin.Parser<char>.Token(c => char.IsDigit(c) || c is '-' or '+' or '.' or 'e' or 'E')
        .AtLeastOnceString();

    static readonly Parser<char, BenchmarkJson> JsonString = String.Select<BenchmarkJson>(x => new BenchmarkJsonString(x));
    static readonly Parser<char, BenchmarkJson> JsonNumber = Number.Select<BenchmarkJson>(x => new BenchmarkJsonNumber(x));
    static readonly Parser<char, BenchmarkJson> JsonBoolean = Pidgin.Parser.String("true").ThenReturn<BenchmarkJson>(new BenchmarkJsonBoolean(true))
        .Or(Pidgin.Parser.String("false").ThenReturn<BenchmarkJson>(new BenchmarkJsonBoolean(false)));
    static readonly Parser<char, BenchmarkJson> JsonNull = Pidgin.Parser.String("null").ThenReturn<BenchmarkJson>(new BenchmarkJsonNull());
    static readonly Parser<char, BenchmarkJson> Json = JsonString.Or(JsonBoolean).Or(JsonNull).Or(JsonNumber)
        .Or(Pidgin.Parser.Rec(
            () => JsonArray ?? throw new InvalidOperationException()))
        .Or(Pidgin.Parser.Rec(
            () => JsonObject ?? throw new InvalidOperationException()));
    static readonly Parser<char, BenchmarkJson> Value = Json.Between(WhiteSpaces);
    static readonly Parser<char, BenchmarkJson> JsonArray = Value.Separated(Pidgin.Parser.Char(','))
        .Between(Pidgin.Parser.Char('['), Pidgin.Parser.Char(']'))
        .Select<BenchmarkJson>(xs => new BenchmarkJsonArray(xs.ToArray()));
    static readonly Parser<char, KeyValuePair<string, BenchmarkJson>> Member = String.Between(WhiteSpaces)
        .Before(Pidgin.Parser.Char(':'))
        .Then(Value, static (key, value) => new KeyValuePair<string, BenchmarkJson>(key, value));
    static readonly Parser<char, BenchmarkJson> JsonObject = Member.Separated(Pidgin.Parser.Char(','))
        .Between(Pidgin.Parser.Char('{'), Pidgin.Parser.Char('}'))
        .Select<BenchmarkJson>(xs => new BenchmarkJsonObject(new Dictionary<string, BenchmarkJson>(xs)));
    static readonly Parser<char, BenchmarkJson> Document = Value.Before(Pidgin.Parser<char>.End);

    public static BenchmarkJson Parse(string input) => Document.ParseOrThrow(input);
}
