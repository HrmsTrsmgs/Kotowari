using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin;
using Sprache;
using Superpower;

namespace Marimo.Kotowari.Benchmarks;

internal static class SuperpowerJsonParser
{
    static TextParser<T> Between<T, U, V>(this TextParser<T> parser, TextParser<U> before, TextParser<V> after)
        => before.IgnoreThen(parser).Then(x => after.Value(x));

    static readonly TextParser<char> WhiteSpace = Superpower.Parsers.Character.WhiteSpace;
    static readonly TextParser<char> Quote = Superpower.Parsers.Character.EqualTo('"');
    static readonly TextParser<string> String = Superpower.Parsers.Character.Matching(c => c != '"' && c != '\\', "string character")
        .Many().Select(string.Concat).Between(Quote, Quote);
    static readonly TextParser<string> Number = Superpower.Parsers.Character.Matching(c => char.IsDigit(c) || c is '-' or '+' or '.' or 'e' or 'E', "number")
        .AtLeastOnce().Select(string.Concat);
    static readonly TextParser<BenchmarkJson> JsonString = String.Select(x => (BenchmarkJson)new BenchmarkJsonString(x));
    static readonly TextParser<BenchmarkJson> JsonNumber = Number.Select(x => (BenchmarkJson)new BenchmarkJsonNumber(x));
    static readonly TextParser<BenchmarkJson> JsonBoolean = Superpower.Parsers.Span.EqualTo("true").Select(_ => (BenchmarkJson)new BenchmarkJsonBoolean(true))
        .Or(Superpower.Parsers.Span.EqualTo("false").Select(_ => (BenchmarkJson)new BenchmarkJsonBoolean(false)));
    static readonly TextParser<BenchmarkJson> JsonNull = Superpower.Parsers.Span.EqualTo("null").Select(_ => (BenchmarkJson)new BenchmarkJsonNull());
    static readonly TextParser<BenchmarkJson> Json = JsonString.Or(JsonBoolean).Or(JsonNull).Or(JsonNumber)
        .Or(Superpower.Parse.Ref(
            () => JsonArray ?? throw new InvalidOperationException()))
        .Or(Superpower.Parse.Ref(
            () => JsonObject ?? throw new InvalidOperationException()));
    static readonly TextParser<BenchmarkJson> Value = Json.Between(WhiteSpace.Many(), WhiteSpace.Many());
    static readonly TextParser<BenchmarkJson> JsonArray = Value.ManyDelimitedBy(Superpower.Parsers.Character.EqualTo(','))
        .Between(Superpower.Parsers.Character.EqualTo('['), Superpower.Parsers.Character.EqualTo(']'))
        .Select(xs => (BenchmarkJson)new BenchmarkJsonArray(xs.ToArray()));
    static readonly TextParser<KeyValuePair<string, BenchmarkJson>> Member = String.Between(WhiteSpace.Many(), WhiteSpace.Many())
        .Then(key => Superpower.Parsers.Character.EqualTo(':').IgnoreThen(Value)
            .Select(value => new KeyValuePair<string, BenchmarkJson>(key, value)));
    static readonly TextParser<BenchmarkJson> JsonObject = Member.ManyDelimitedBy(Superpower.Parsers.Character.EqualTo(','))
        .Between(Superpower.Parsers.Character.EqualTo('{'), Superpower.Parsers.Character.EqualTo('}'))
        .Select(xs => (BenchmarkJson)new BenchmarkJsonObject(new Dictionary<string, BenchmarkJson>(xs)));

    public static BenchmarkJson Parse(string input) => Value.AtEnd().Parse(input);
}
