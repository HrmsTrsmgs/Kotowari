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


public class JSON
{
    static Parser<char> WhiteSpace =
        new OrParser<char>(
            new CharParser(' '),
            new CharParser('\t'),
            new CharParser('\r'),
            new CharParser('\n'));

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

    static Parser<JSONLiteral> JNull =
        new ParserConverter<string, JSONLiteral>(
            new WordParser("null", true, WhiteSpace),
            word => new JSONLiteral(LiteralType.Null));

    static Parser<JSONLiteral> JBoolean =
        new ParserConverter<string, JSONLiteral>(
            new OrParser<string>(
                new WordParser("true", true, WhiteSpace),
                new WordParser("false", true, WhiteSpace)),
            word => new JSONLiteral(word, LiteralType.Boolean));

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

    static Parser<JSONLiteral> JNumber =
        new WithWhiteSpaceParser<JSONLiteral>(
            WhiteSpace,
            new ParserConverter<(string, Optional<string>, Optional<string>), JSONLiteral>(
                new SequenceParser<string, Optional<string>, Optional<string>>(
                    JInt,
                    new OptionalParser<string>(JFrac),
                    new OptionalParser<string>(JExp)),
                tuple =>
                    new JSONLiteral(
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

    static Parser<char> JChar =
        new OrParser<char>(
            new ExpectCharParser(
                new OrParser<char>(
                    DoubleQuote,
                    BackSlash)),
            ControlChar);

    static Parser<JSONLiteral> JString =
        new WithWhiteSpaceParser<JSONLiteral>(
            WhiteSpace,
            new ParserConverter<(char, IEnumerable<char>, char), JSONLiteral>(
                new SequenceParser<char, IEnumerable<char>, char>(
                    DoubleQuote,
                    new ZeroOrMoreParser<char>(JChar),
                    DoubleQuote),
                tuple => new JSONLiteral(new string([.. tuple.Item2]), LiteralType.String)));

    static Parser<JSONLiteral> JLiteral =
        new OrParser<JSONLiteral>(
            JString,
            JBoolean,
            JNull,
            JNumber);

    static Parser<IJSONValue> JValue =
        new OrParser<IJSONValue>(
            new RecursiveParser<IJSONValue>(
                () => new ParserConverter<JSONArray, IJSONValue>(JArray, array => array)),
            new RecursiveParser<IJSONValue>(
                () => new ParserConverter<JSONObject, IJSONValue>(JObject, obj => obj)),
            new ParserConverter<JSONLiteral, IJSONValue>(JLiteral, literal => literal));

    static Parser<IEnumerable<IJSONValue>> JElements =
        new DelimitedSequenceParser<IJSONValue, char>(
            JValue,
            CommaSign);

    static Parser<JSONArray> JArray =
        new ParserConverter<(char, IEnumerable<IJSONValue>, char), JSONArray>(
            new SequenceParser<char, IEnumerable<IJSONValue>, char>(
                BracketOpenSign,
                JElements,
                BracketCloseSign),
            tuple => new JSONArray(tuple.Item2.Any() ? tuple.Item2 : null));


    static Parser<KeyValuePair<string, IJSONValue>> JPair =
        new ParserConverter<(JSONLiteral, char, IJSONValue), KeyValuePair<string, IJSONValue>>(
            new SequenceParser<JSONLiteral, char, IJSONValue>(
                        JString,
                        CollonSign,
                        JValue),
            tuple => new KeyValuePair<string, IJSONValue>(tuple.Item1.Value, tuple.Item3));

    static Parser<JSONObject> JObject =
        new ParserConverter<(char, IEnumerable<KeyValuePair<string, IJSONValue>>, char), JSONObject>(
            new SequenceParser<char, IEnumerable<KeyValuePair<string, IJSONValue>>, char>(
                BraceOpenSign,
                new DelimitedSequenceParser<KeyValuePair<string, IJSONValue>, char>(
                    JPair,
                    CommaSign),
                BraceCloseSign),
            tuple => new JSONObject { Pairs = tuple.Item2.ToDictionary(kv => kv.Key, kv => kv.Value) });

    public static JSONObject Parse(string text)
    {
        var result = JObject.Parse(new Cursol(text));
        if(result.IsSuccess)
        {
            return result.Parsed;
        }
        throw new ParseException();
    }
}
