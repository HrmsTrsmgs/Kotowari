using System.Collections.Generic;
using System.Linq;
using Pidgin;
using Sprache;
using Superpower;

namespace Marimo.Kotowari.Benchmarks;

internal static class SpracheJsonParser
{
    static readonly Sprache.Parser<IEnumerable<char>> WhiteSpaces = Sprache.Parse.WhiteSpace.Many();
    static readonly Sprache.Parser<char> Quote = Sprache.Parse.Char('"');
    static readonly Sprache.Parser<string> String = Sprache.Parse.Char(c => c != '"' && c != '\\', "string character")
        .Many().Text().Contained(Quote, Quote).Token();
    static readonly Sprache.Parser<string> Number = Sprache.Parse.Char(c => char.IsDigit(c) || c is '-' or '+' or '.' or 'e' or 'E', "number")
        .AtLeastOnce().Text().Token();
    static readonly Sprache.Parser<BenchmarkJson> JsonString = String.Select(x => (BenchmarkJson)new BenchmarkJsonString(x));
    static readonly Sprache.Parser<BenchmarkJson> JsonNumber = Number.Select(x => (BenchmarkJson)new BenchmarkJsonNumber(x));
    static readonly Sprache.Parser<BenchmarkJson> JsonBoolean = Sprache.Parse.String("true").Select(_ => (BenchmarkJson)new BenchmarkJsonBoolean(true))
        .Or(Sprache.Parse.String("false").Select(_ => (BenchmarkJson)new BenchmarkJsonBoolean(false)));
    static readonly Sprache.Parser<BenchmarkJson> JsonNull = Sprache.Parse.String("null").Select(_ => (BenchmarkJson)new BenchmarkJsonNull());
    static readonly Sprache.Parser<BenchmarkJson> Json = JsonString.Or(JsonBoolean).Or(JsonNull).Or(JsonNumber)
        .Or(Sprache.Parse.Ref(() => JsonArray)).Or(Sprache.Parse.Ref(() => JsonObject));
    static readonly Sprache.Parser<BenchmarkJson> Value = Json;
    static readonly Sprache.Parser<IEnumerable<BenchmarkJson>> Values = Value.DelimitedBy(Sprache.Parse.Char(',').Token())
        .Optional().Select(xs => xs.IsDefined ? xs.Get() : []);
    static readonly Sprache.Parser<BenchmarkJson> JsonArray = Values
        .Contained(Sprache.Parse.Char('[').Token(), Sprache.Parse.Char(']').Token())
        .Select(xs => (BenchmarkJson)new BenchmarkJsonArray(xs.ToArray()));
    static readonly Sprache.Parser<KeyValuePair<string, BenchmarkJson>> Member =
        from key in String
        from colon in Sprache.Parse.Char(':').Token()
        from value in Value
        select new KeyValuePair<string, BenchmarkJson>(key, value);
    static readonly Sprache.Parser<IEnumerable<KeyValuePair<string, BenchmarkJson>>> Members = Member.DelimitedBy(Sprache.Parse.Char(',').Token())
        .Optional().Select(xs => xs.IsDefined ? xs.Get() : []);
    static readonly Sprache.Parser<BenchmarkJson> JsonObject = Members
        .Contained(Sprache.Parse.Char('{').Token(), Sprache.Parse.Char('}').Token())
        .Select(xs => (BenchmarkJson)new BenchmarkJsonObject(new Dictionary<string, BenchmarkJson>(xs)));
    static readonly Sprache.Parser<BenchmarkJson> Document = WhiteSpaces.Then(_ => Value).End();

    public static BenchmarkJson Parse(string input)
        => Document.Parse(input);
}
