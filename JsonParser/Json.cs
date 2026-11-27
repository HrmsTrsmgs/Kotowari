using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marimo.Parser;

/// <summary>Parses, maps and serializes JSON using the SpracheJson sample's conventions.</summary>
public class Json
{
    /// <summary>Maps a JSON value using the upstream sample's mapping contract.</summary>
    /// <typeparam name="T">The destination CLR type.</typeparam>
    /// <param name="toMap">The JSON value to map.</param>
    /// <returns>The mapped non-null value; JSON numbers map to double without numeric coercion.</returns>
    /// <exception cref="ArgumentException">The mapped value cannot be returned as T.</exception>
    public static T Map<T>(IJsonValue toMap) =>
        JsonMapper.MapValue(typeof(T), toMap) is T mapped
            ? mapped
            : throw new ArgumentException($"Cannot map value to type {typeof(T)}.", nameof(toMap));

    /// <summary>Parses an object and maps it using the upstream sample's mapping contract.</summary>
    /// <typeparam name="T">The destination CLR type.</typeparam>
    /// <param name="toMap">The JSON object text.</param>
    /// <returns>The mapped object.</returns>
    public static T Deserialize<T>(string toMap) => Map<T>(Parse(toMap));

    /// <summary>Serializes a value using the upstream sample's serialization contract.</summary>
    /// <typeparam name="T">The declared CLR type used for serialization.</typeparam>
    /// <param name="toWrite">A supported CLR value or JSON value.</param>
    /// <returns>The upstream-style JSON representation.</returns>
    /// <exception cref="ArgumentException">A primitive type other than double or bool is requested.</exception>
    public static string Serialize<T>(T toWrite) =>
        toWrite is IJsonValue value ? value.ToJson() : JsonSerializer.SerializeValue(typeof(T), toWrite);

    // 参照元のTokenと同じUnicode空白を読み飛ばす。
    static readonly Parser<char> WhiteSpace =
        new ConditionalCharParser(char.IsWhiteSpace);

    static readonly Parser<char> DoubleQuote = new CharParser('"');

    static readonly Parser<char> Dot = new CharParser('.');

    static readonly Parser<char> Plus = new CharParser('+');

    static readonly Parser<char> Minus = new CharParser('-');

    static readonly Parser<char> BackSlash = new CharParser('\\');

    // Structural punctuation consumes trailing whitespace; characters inside strings and numbers do not.
    static Parser<char> Punctuation(char character) => new CharParser(character).WithWhiteSpace(WhiteSpace);

    static readonly Parser<char> BraceOpenSign = Punctuation('{');
    static readonly Parser<char> BraceCloseSign = Punctuation('}');
    static readonly Parser<char> BracketOpenSign = Punctuation('[');
    static readonly Parser<char> BracketCloseSign = Punctuation(']');
    static readonly Parser<char> ColonSign = Punctuation(':');
    static readonly Parser<char> CommaSign = Punctuation(',');

    static readonly Parser<JsonLiteral> JNull =
        new ParserConverter<string, JsonLiteral>(
            new WordParser("null", true).WithWhiteSpace(WhiteSpace),
            _ => new JsonLiteral(LiteralType.Null));

    static readonly Parser<JsonLiteral> JBoolean =
        new ParserConverter<string, JsonLiteral>(
            new OrParser<string>(
                new WordParser("true", true),
                new WordParser("false", true)).WithWhiteSpace(WhiteSpace),
            word => new JsonLiteral(word, LiteralType.Boolean));

    static readonly Parser<char> Digit =
        new ConditionalCharParser(char.IsDigit);

    static readonly Parser<string> Digits =
        new ParserConverter<IEnumerable<char>, string>(
            new ZeroOrMoreParser<char>(Digit),
            chars => new string([.. chars]));

    static readonly Parser<string> JExp =
        new ParserConverter<(char, Optional<char>, string), string>(
            new SequenceParser<char, Optional<char>, string>(
                new CharParser('e', true),
                new OptionalParser<char>(
                    new OrParser<char>(Plus, Minus)),
                Digits),
            tuple => tuple.Item1.ToString()
            + (tuple.Item2.IsPresent ? tuple.Item2.Value.ToString() : "")
            + tuple.Item3);

    static readonly Parser<string> JFrac =
        new ParserConverter<(char, string), string>(
            new SequenceParser<char, string>(Dot, Digits),
            tuple => $"{tuple.Item1}{tuple.Item2}");

    static readonly Parser<string> JInt =
        new ParserConverter<(Optional<char>, string), string>(
            new SequenceParser<Optional<char>, string>(
                new OptionalParser<char>(new CharParser('-')),
                Digits),
            tuple => $"{(tuple.Item1.IsPresent ? "-" : "")}{tuple.Item2}");

    static readonly Parser<JsonLiteral> JNumber =
        new ParserConverter<(string, Optional<string>, Optional<string>), JsonLiteral>(
            new SequenceParser<string, Optional<string>, Optional<string>>(
                JInt,
                new OptionalParser<string>(JFrac),
                new OptionalParser<string>(JExp)),
            tuple =>
                new JsonLiteral(
                    tuple.Item1
                    + (tuple.Item2.IsPresent ? tuple.Item2.Value : "")
                    + (tuple.Item3.IsPresent ? tuple.Item3.Value : ""),
                    LiteralType.Number)).WithWhiteSpace(WhiteSpace);

    static Parser<char> Escaped(char encoded, char decoded)
        => ParserConverter.Create(new CharParser(encoded), _ => decoded);

    static readonly Parser<char> ControlChar =
        new ParserConverter<(char, char), char>(
            new SequenceParser<char, char>(
                BackSlash,
                new OrParser<char>(
                    Escaped('"', '"'),
                    Escaped('\\', '\\'),
                    Escaped('b', '\b'),
                    Escaped('n', '\n'),
                    Escaped('f', '\f'),
                    Escaped('r', '\r'),
                    Escaped('t', '\t'))),
            tuple => tuple.Item2);

    // 固定2文字の除外をまとめ、通常文字ごとの候補走査と文字の再取得を省く。
    static readonly Parser<char> JChar =
        new JsonStringCharacterParser(
            new OrParser<char>(
                new ConditionalCharParser(it => it is not ('"' or '\\')),
                ControlChar));

    static readonly Parser<JsonLiteral> JString =
        new ParserConverter<(char, IEnumerable<char>, char), JsonLiteral>(
            new SequenceParser<char, IEnumerable<char>, char>(
                DoubleQuote,
                new ZeroOrMoreParser<char>(JChar),
                DoubleQuote),
            tuple => new JsonLiteral(new string([.. tuple.Item2]), LiteralType.String)).WithWhiteSpace(WhiteSpace);

    static readonly Parser<JsonLiteral> JLiteral =
        new OrParser<JsonLiteral>(
            JString,
            JBoolean,
            JNull,
            JNumber);

    static readonly Parser<IJsonValue> JValue =
        new OrParser<IJsonValue>(
            new RecursiveParser<IJsonValue>(
                () => new ParserConverter<JsonArray, IJsonValue>(
                    JArray ?? throw new InvalidOperationException(),
                    array => array)),
            new RecursiveParser<IJsonValue>(
                () => new ParserConverter<JsonObject, IJsonValue>(
                    JObject ?? throw new InvalidOperationException(),
                    obj => obj)),
            new ParserConverter<JsonLiteral, IJsonValue>(JLiteral, literal => literal));

    static readonly Parser<IEnumerable<IJsonValue>> JElements =
        new DelimitedSequenceParser<IJsonValue, char>(
            JValue,
            CommaSign);

    static readonly Parser<JsonArray> JArray =
        new ParserConverter<(char, IEnumerable<IJsonValue>, char), JsonArray>(
            new SequenceParser<char, IEnumerable<IJsonValue>, char>(
                BracketOpenSign,
                JElements,
                BracketCloseSign),
            tuple => new JsonArray(tuple.Item2));


    static readonly Parser<string> JKey =
        new ParserConverter<(char, IEnumerable<char>, char), string>(
            new SequenceParser<char, IEnumerable<char>, char>(
                DoubleQuote,
                new OneOrMoreParser<char>(JChar),
                DoubleQuote),
            tuple => new string([.. tuple.Item2])).WithWhiteSpace(WhiteSpace);

    static readonly Parser<KeyValuePair<string, IJsonValue>> JPair =
        new ParserConverter<(string, char, IJsonValue), KeyValuePair<string, IJsonValue>>(
            new SequenceParser<string, char, IJsonValue>(
                JKey,
                ColonSign,
                JValue),
            tuple => new(tuple.Item1, tuple.Item3));

    static readonly Parser<JsonObject> JObject =
        new ParserConverter<(char, IEnumerable<KeyValuePair<string, IJsonValue>>, char), JsonObject>(
            new SequenceParser<char, IEnumerable<KeyValuePair<string, IJsonValue>>, char>(
                BraceOpenSign,
                new DelimitedSequenceParser<KeyValuePair<string, IJsonValue>, char>(
                    JPair,
                    CommaSign),
                BraceCloseSign),
            tuple => new JsonObject { Pairs = tuple.Item2.ToDictionary(kv => kv.Key, kv => kv.Value) });

    static readonly Parser<JsonObject> Document = new WithWhiteSpaceParser<JsonObject>(WhiteSpace, JObject);

    /// <summary>Parses the leading JSON object without requiring the input to end after it.</summary>
    /// <param name="text">The input text, optionally starting with whitespace.</param>
    /// <returns>The parsed object.</returns>
    /// <exception cref="ParseException">The object parser reports failure.</exception>
    public static JsonObject Parse(string text)
    {
        var result = ParseDocument(text);
        return result.IsSuccess ? result.Parsed : throw new ParseException();
    }

    /// <summary>Keeps the final cursor available for callers that verify full input consumption.</summary>
    internal static ParseResult<JsonObject> ParseDocument(string text)
        => Document.Parse(new Cursol(text));
}
