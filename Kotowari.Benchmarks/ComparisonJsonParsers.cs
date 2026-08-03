using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin;
using Sprache;
using Superpower;
using Superpower.Parsers;

namespace Marimo.Kotowari.Benchmarks;

internal abstract record BenchmarkJson;
internal sealed record BenchmarkJsonObject(IReadOnlyDictionary<string, BenchmarkJson> Members) : BenchmarkJson;
internal sealed record BenchmarkJsonArray(IReadOnlyList<BenchmarkJson> Elements) : BenchmarkJson;
internal sealed record BenchmarkJsonString(string Value) : BenchmarkJson;
internal sealed record BenchmarkJsonNumber(string Value) : BenchmarkJson;
internal sealed record BenchmarkJsonBoolean(bool Value) : BenchmarkJson;
internal sealed record BenchmarkJsonNull : BenchmarkJson;

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
        .Or(Pidgin.Parser.Rec(() => JsonArray)).Or(Pidgin.Parser.Rec(() => JsonObject));
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

internal static class SpracheJsonParser
{
    private static readonly Sprache.Parser<IEnumerable<char>> WhiteSpaces = Sprache.Parse.WhiteSpace.Many();
    private static readonly Sprache.Parser<char> Quote = Sprache.Parse.Char('"');
    private static readonly Sprache.Parser<string> String = Sprache.Parse.Char(c => c != '"' && c != '\\', "string character")
        .Many().Text().Contained(Quote, Quote).Token();
    private static readonly Sprache.Parser<string> Number = Sprache.Parse.Char(c => char.IsDigit(c) || c is '-' or '+' or '.' or 'e' or 'E', "number")
        .AtLeastOnce().Text().Token();
    private static readonly Sprache.Parser<BenchmarkJson> JsonString = String.Select(x => (BenchmarkJson)new BenchmarkJsonString(x));
    private static readonly Sprache.Parser<BenchmarkJson> JsonNumber = Number.Select(x => (BenchmarkJson)new BenchmarkJsonNumber(x));
    private static readonly Sprache.Parser<BenchmarkJson> JsonBoolean = Sprache.Parse.String("true").Select(_ => (BenchmarkJson)new BenchmarkJsonBoolean(true))
        .Or(Sprache.Parse.String("false").Select(_ => (BenchmarkJson)new BenchmarkJsonBoolean(false)));
    private static readonly Sprache.Parser<BenchmarkJson> JsonNull = Sprache.Parse.String("null").Select(_ => (BenchmarkJson)new BenchmarkJsonNull());
    private static readonly Sprache.Parser<BenchmarkJson> Json = JsonString.Or(JsonBoolean).Or(JsonNull).Or(JsonNumber)
        .Or(Sprache.Parse.Ref(() => JsonArray)).Or(Sprache.Parse.Ref(() => JsonObject));
    private static readonly Sprache.Parser<BenchmarkJson> Value = Json;
    private static readonly Sprache.Parser<IEnumerable<BenchmarkJson>> Values = Value.DelimitedBy(Sprache.Parse.Char(',').Token())
        .Optional().Select(xs => xs.IsDefined ? xs.Get() : Enumerable.Empty<BenchmarkJson>());
    private static readonly Sprache.Parser<BenchmarkJson> JsonArray = Values
        .Contained(Sprache.Parse.Char('[').Token(), Sprache.Parse.Char(']').Token())
        .Select(xs => (BenchmarkJson)new BenchmarkJsonArray(xs.ToArray()));
    private static readonly Sprache.Parser<KeyValuePair<string, BenchmarkJson>> Member =
        from key in String
        from colon in Sprache.Parse.Char(':').Token()
        from value in Value
        select new KeyValuePair<string, BenchmarkJson>(key, value);
    private static readonly Sprache.Parser<IEnumerable<KeyValuePair<string, BenchmarkJson>>> Members = Member.DelimitedBy(Sprache.Parse.Char(',').Token())
        .Optional().Select(xs => xs.IsDefined ? xs.Get() : Enumerable.Empty<KeyValuePair<string, BenchmarkJson>>());
    private static readonly Sprache.Parser<BenchmarkJson> JsonObject = Members
        .Contained(Sprache.Parse.Char('{').Token(), Sprache.Parse.Char('}').Token())
        .Select(xs => (BenchmarkJson)new BenchmarkJsonObject(new Dictionary<string, BenchmarkJson>(xs)));
    private static readonly Sprache.Parser<BenchmarkJson> Document = WhiteSpaces.Then(_ => Value).End();

    public static BenchmarkJson Parse(string input)
        => Document.Parse(input);
}

internal static class SuperpowerJsonParser
{
    private static TextParser<T> Between<T, U, V>(this TextParser<T> parser, TextParser<U> before, TextParser<V> after)
        => before.IgnoreThen(parser).Then(x => after.Value(x));

    private static readonly TextParser<char> WhiteSpace = Superpower.Parsers.Character.WhiteSpace;
    private static readonly TextParser<char> Quote = Superpower.Parsers.Character.EqualTo('"');
    private static readonly TextParser<string> String = Superpower.Parsers.Character.Matching(c => c != '"' && c != '\\', "string character")
        .Many().Select(string.Concat).Between(Quote, Quote);
    private static readonly TextParser<string> Number = Superpower.Parsers.Character.Matching(c => char.IsDigit(c) || c is '-' or '+' or '.' or 'e' or 'E', "number")
        .AtLeastOnce().Select(string.Concat);
    private static readonly TextParser<BenchmarkJson> JsonString = String.Select(x => (BenchmarkJson)new BenchmarkJsonString(x));
    private static readonly TextParser<BenchmarkJson> JsonNumber = Number.Select(x => (BenchmarkJson)new BenchmarkJsonNumber(x));
    private static readonly TextParser<BenchmarkJson> JsonBoolean = Superpower.Parsers.Span.EqualTo("true").Select(_ => (BenchmarkJson)new BenchmarkJsonBoolean(true))
        .Or(Superpower.Parsers.Span.EqualTo("false").Select(_ => (BenchmarkJson)new BenchmarkJsonBoolean(false)));
    private static readonly TextParser<BenchmarkJson> JsonNull = Superpower.Parsers.Span.EqualTo("null").Select(_ => (BenchmarkJson)new BenchmarkJsonNull());
    private static readonly TextParser<BenchmarkJson> Json = JsonString.Or(JsonBoolean).Or(JsonNull).Or(JsonNumber)
        .Or(Superpower.Parse.Ref(() => JsonArray)).Or(Superpower.Parse.Ref(() => JsonObject));
    private static readonly TextParser<BenchmarkJson> Value = Json.Between(WhiteSpace.Many(), WhiteSpace.Many());
    private static readonly TextParser<BenchmarkJson> JsonArray = Value.ManyDelimitedBy(Superpower.Parsers.Character.EqualTo(','))
        .Between(Superpower.Parsers.Character.EqualTo('['), Superpower.Parsers.Character.EqualTo(']'))
        .Select(xs => (BenchmarkJson)new BenchmarkJsonArray(xs.ToArray()));
    private static readonly TextParser<KeyValuePair<string, BenchmarkJson>> Member = String.Between(WhiteSpace.Many(), WhiteSpace.Many())
        .Then(key => Superpower.Parsers.Character.EqualTo(':').IgnoreThen(Value)
            .Select(value => new KeyValuePair<string, BenchmarkJson>(key, value)));
    private static readonly TextParser<BenchmarkJson> JsonObject = Member.ManyDelimitedBy(Superpower.Parsers.Character.EqualTo(','))
        .Between(Superpower.Parsers.Character.EqualTo('{'), Superpower.Parsers.Character.EqualTo('}'))
        .Select(xs => (BenchmarkJson)new BenchmarkJsonObject(new Dictionary<string, BenchmarkJson>(xs)));

    public static BenchmarkJson Parse(string input) => Value.AtEnd().Parse(input);
}
