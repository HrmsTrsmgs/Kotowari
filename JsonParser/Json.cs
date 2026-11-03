using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Marimo.Parser;


public class Json
{
    // 候補ごとの文字取得を繰り返さず、JSONの空白4文字をまとめて判定する。
    static Parser<char> WhiteSpace =
        new ConditionalCharParser(it => it is ' ' or '\t' or '\r' or '\n');

    static Parser<char> BraceOpen = new CharParser('{');
    
    static Parser<char> BraceClose = new CharParser('}');

    static Parser<char> BracketOpen = new CharParser('[');

    static Parser<char> BracketClose = new CharParser(']');

    static Parser<char> DoubleQuote = new CharParser('"');

    static Parser<char> Collon = new CharParser(':');

    static Parser<char> Dot = new CharParser('.');

    static Parser<char> Plus = new CharParser('+');

    static Parser<char> Minus = new CharParser('-');

    static Parser<char> BackSlash = new CharParser('\\');

    static Parser<char> Comma = new CharParser(',');

    static Parser<char> BraceOpenSign =
        new WithWhiteSpaceParser<char>(
            WhiteSpace,
            BraceOpen);
    static Parser<char> BraceCloseSign =
        new WithWhiteSpaceParser<char>(
            WhiteSpace,
            BraceClose);

    static Parser<char> BracketOpenSign =
        new WithWhiteSpaceParser<char>(
            WhiteSpace,
            BracketOpen);

    static Parser<char> BracketCloseSign =
        new WithWhiteSpaceParser<char>(
            WhiteSpace,
            BracketClose);

    static Parser<char> CollonSign =
        new WithWhiteSpaceParser<char>(
            WhiteSpace,
            Collon);

    static Parser<char> CommaSign =
        new WithWhiteSpaceParser<char>(
            WhiteSpace,
            Comma);

    static Parser<JsonLiteral> JNull =
        new ParserConverter<string, JsonLiteral>(
            new WordParser("null", true, WhiteSpace),
            word => new JsonLiteral(LiteralType.Null));

    static Parser<JsonLiteral> JBoolean =
        new ParserConverter<string, JsonLiteral>(
            new OrParser<string>(
                new WordParser("true", true, WhiteSpace),
                new WordParser("false", true, WhiteSpace)),
            word => new JsonLiteral(word, LiteralType.Boolean));

    static Parser<char> Digit =
        new ConditionalCharParser(char.IsDigit);

    static Parser<string> Digits =
        new ParserConverter<IEnumerable<char>, string>(
            new OneOrMoreParser<char>(Digit),
            chars => new string([.. chars]));

    static Parser<string> JExp =
        new ParserConverter<(char, Optional<char>, string), string>(
            new SequenceParser<char, Optional<char>, string>(
                new CharParser('e', true),
                new OptionalParser<char>(
                    new OrParser<char>(Plus, Minus)),
                Digits),
            tuple => tuple.Item1.ToString()
            + (tuple.Item2.IsPresent ? tuple.Item2.Value.ToString() : "")
            + tuple.Item3);

    static Parser<string> JFrac =
        new ParserConverter<(char, string), string>(
            new SequenceParser<char, string>(Dot, Digits),
            tuple => $"{tuple.Item1}{tuple.Item2}");

    static Parser<string> JInt =
        new ParserConverter<(Optional<char>, string), string>(
            new SequenceParser<Optional<char>, string>(
                new OptionalParser<char>(new CharParser('-')),
                Digits),
            tuple => $"{(tuple.Item1.IsPresent ? "-" : "")}{tuple.Item2}");

    static Parser<JsonLiteral> JNumber =
        new WithWhiteSpaceParser<JsonLiteral>(
            WhiteSpace,
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
                        LiteralType.Number)));

    static Parser<char> ControlChar =
        new ParserConverter<(char, char), char>(
            new SequenceParser<char, char>(
                BackSlash,
                new OrParser<char>(
                    DoubleQuote,
                    BackSlash,
                    new CharParser('b'),
                    new CharParser('n'),
                    new CharParser('f'),
                    new CharParser('r'),
                    new CharParser('t'))),
            tuple => tuple.Item2 switch
            {
                'b' => '\b',
                'n' => '\n',
                'f' => '\f',
                'r' => '\r',
                't' => '\t',
                var c => c
            });

    // 固定2文字の除外をまとめ、通常文字ごとの候補走査と文字の再取得を省く。
    static Parser<char> JChar =
        new JsonStringCharacterParser(
            new OrParser<char>(
                new ConditionalCharParser(it => it is not ('"' or '\\')),
                ControlChar));

    static Parser<JsonLiteral> JString =
        new WithWhiteSpaceParser<JsonLiteral>(
            WhiteSpace,
            new ParserConverter<(char, IEnumerable<char>, char), JsonLiteral>(
                new SequenceParser<char, IEnumerable<char>, char>(
                    DoubleQuote,
                    new ZeroOrMoreParser<char>(JChar),
                    DoubleQuote),
                tuple => new JsonLiteral(new string([.. tuple.Item2]), LiteralType.String)));

    static Parser<JsonLiteral> JLiteral =
        new OrParser<JsonLiteral>(
            JString,
            JBoolean,
            JNull,
            JNumber);

    static Parser<IJsonValue> JValue =
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

    static Parser<IEnumerable<IJsonValue>> JElements =
        new DelimitedSequenceParser<IJsonValue, char>(
            JValue,
            CommaSign);

    static Parser<JsonArray> JArray =
        new ParserConverter<(char, IEnumerable<IJsonValue>, char), JsonArray>(
            new SequenceParser<char, IEnumerable<IJsonValue>, char>(
                BracketOpenSign,
                JElements,
                BracketCloseSign),
            tuple => new JsonArray(tuple.Item2));


    static Parser<KeyValuePair<string, IJsonValue>> JPair =
        new ParserConverter<(JsonLiteral, char, IJsonValue), KeyValuePair<string, IJsonValue>>(
            new SequenceParser<JsonLiteral, char, IJsonValue>(
                        JString,
                        CollonSign,
                        JValue),
            tuple => new KeyValuePair<string, IJsonValue>(
                tuple.Item1.Value ?? throw new InvalidOperationException(),
                tuple.Item3));

    static Parser<JsonObject> JObject =
        new ParserConverter<(char, IEnumerable<KeyValuePair<string, IJsonValue>>, char), JsonObject>(
            new SequenceParser<char, IEnumerable<KeyValuePair<string, IJsonValue>>, char>(
                BraceOpenSign,
                new DelimitedSequenceParser<KeyValuePair<string, IJsonValue>, char>(
                    JPair,
                    CommaSign),
                BraceCloseSign),
            tuple => new JsonObject { Pairs = tuple.Item2.ToDictionary(kv => kv.Key, kv => kv.Value) });

    public static JsonObject Parse(string text)
    {
        var result = JObject.Parse(new Cursol(text));
        if(result.IsSuccess)
        {
            return result.Parsed;
        }
        throw new ParseException();
    }
}
