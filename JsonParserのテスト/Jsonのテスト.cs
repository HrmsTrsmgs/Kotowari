using FluentAssertions;
using System;
using Xunit;

#if SPRACHE_JSON_REFERENCE
using IJsonValue = SpracheJson.IJsonValue;
using Json = SpracheJson.Json;
using JsonArray = SpracheJson.JsonArray;
using JsonLiteral = SpracheJson.JsonLiteral;
using JsonObject = SpracheJson.JsonObject;
using LiteralType = SpracheJson.JsonLiteralType;
using ParseException = Sprache.ParseException;
#else
using IJsonValue = Marimo.Parser.IJsonValue;
using Json = Marimo.Parser.Json;
using JsonArray = Marimo.Parser.JsonArray;
using JsonLiteral = Marimo.Parser.JsonLiteral;
using JsonObject = Marimo.Parser.JsonObject;
using LiteralType = Marimo.Parser.LiteralType;
using ParseException = Marimo.Parser.ParseException;
#endif

namespace Marimo.Parser.Test;

public class Jsonのテスト
{
    // This source is compiled for both implementations; only the type aliases above differ.
    // Fixed upstream: IanWold/SpracheJson aa8092aa4f5a7f1675200ac908d20c06e285aec4.

    [Theory]
    [InlineData("-")]
    [InlineData("1e")]
    [InlineData("1e+")]
    public void 数字が不足した符号や指数部も数値として受理します(string number)
    {
        var tested = Json.Parse($"{{\"a\":{number}}}")["a"].Should().BeOfType<JsonLiteral>().Which;

        tested.ValueType.Should().Be(LiteralType.Number);
        tested.Value.Should().Be(number);
    }

    [Theory]
    [InlineData("01")]
    [InlineData("١٢")]
    public void 先頭のゼロやUnicode数字も数値の文字列として保持します(string number)
    {
        var tested = Json.Parse($"{{\"a\":{number}}}")["a"].Should().BeOfType<JsonLiteral>().Which;

        tested.ValueType.Should().Be(LiteralType.Number);
        tested.Value.Should().Be(number);
    }

    [Theory]
    [InlineData("""{"a":[1,]}""", "1", "")]
    [InlineData("""{"a":[,]}""", "", "")]
    public void 配列の省略された要素を空の数値として読み込みます(string text, string first, string second)
    {
        var array = Json.Parse(text)["a"].Should().BeOfType<JsonArray>().Which;

        array.Elements.Should().HaveCount(2);
        var firstValue = array[0].Should().BeOfType<JsonLiteral>().Which;
        firstValue.ValueType.Should().Be(LiteralType.Number);
        firstValue.Value.Should().Be(first);
        var secondValue = array[1].Should().BeOfType<JsonLiteral>().Which;
        secondValue.ValueType.Should().Be(LiteralType.Number);
        secondValue.Value.Should().Be(second);
    }

    [Theory]
    [InlineData("{}suffix")]
    [InlineData("{} {}")]
    public void オブジェクトの後に入力が続いても解析結果を返します(string text)
    {
        Json.Parse(text).Pairs.Should().BeEmpty();
    }

    [Fact]
    public void 引用符内の改行を文字列として保持します()
    {
        var tested = Json.Parse("{\"a\":\"\n\"}")["a"].Should().BeOfType<JsonLiteral>().Which;

        tested.ValueType.Should().Be(LiteralType.String);
        tested.Value.Should().Be("\n");
    }

    [Fact]
    public void 空文字列の値を読み込みます()
    {
        var tested = Json.Parse("""{"a":""}""")["a"].Should().BeOfType<JsonLiteral>().Which;

        tested.ValueType.Should().Be(LiteralType.String);
        tested.Value.Should().BeEmpty();
    }

    [Fact]
    public void ルートの配列を拒否します()
    {
        var parse = () => Json.Parse("[]");

        parse.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("""{"a":"\/"}""")]
    [InlineData("""{"a":"\u0061"}""")]
    public void 未対応のスラッシュとUnicodeのエスケープを拒否します(string text)
    {
        var parse = () => Json.Parse(text);

        parse.Should().Throw<Exception>();
    }

    [Fact]
    public void 重複したキーを持つオブジェクトを拒否します()
    {
        var parse = () => Json.Parse("""{"a":1,"a":2}""");

        parse.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("/\"\\\b\f\n\r\t", "\"\\/\\\"\\\\\\b\\f\\n\\r\\t\"")]
    [InlineData(null, "\"\"")]
    public void 文字列リテラルは文字列で取得しJSONではエスケープします(string? text, string json)
    {
        var tested = text is null
            ? new JsonLiteral("", LiteralType.String) { Value = null }
            : new JsonLiteral(text, LiteralType.String);

        if (text is null) tested.Get().Should().BeNull();
        else tested.Get().Should().BeOfType<string>().Which.Should().Be(text);
        tested.ToString().Should().Be(text ?? "");
        tested.ToJson().Should().Be(json);
    }

    [Theory]
    [InlineData("12", 12d)]
    [InlineData(null, 0d)]
    public void 数値リテラルはdoubleで取得し元の文字列表現を返します(string? text, double number)
    {
        var tested = text is null
            ? new JsonLiteral("", LiteralType.Number) { Value = null }
            : new JsonLiteral(text, LiteralType.Number);

        tested.Get().Should().BeOfType<double>().Which.Should().Be(number);
        tested.ToString().Should().Be(text ?? "");
        tested.ToJson().Should().Be(text ?? "");
    }

    [Theory]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public void 真偽値リテラルはboolで取得し元の文字列表現を返します(string? text, bool boolean)
    {
        var tested = text is null
            ? new JsonLiteral("", LiteralType.Boolean) { Value = null }
            : new JsonLiteral(text, LiteralType.Boolean);

        tested.Get().Should().BeOfType<bool>().Which.Should().Be(boolean);
        tested.ToString().Should().Be(text ?? "");
        tested.ToJson().Should().Be(text ?? "");
    }

    [Fact]
    public void Nullリテラルはnullで取得し空の表示文字列とnullのJSON表現を返します()
    {
        var tested = new JsonLiteral("", LiteralType.Null) { Value = null };

        tested.Get().Should().BeNull();
        tested.ToString().Should().BeEmpty();
        tested.ToJson().Should().Be("null");
    }

    [Fact]
    public void ネストしたオブジェクトと配列をJSON文字列へ戻します()
    {
        const string text = """{"a":[1,"b"]}""";

        Json.Parse(text).ToJson().Should().Be(
            "{\r\n\t\"a\": [\r\n\t\t1,\r\n\t\t\"b\"\r\n\t]\r\n}");
    }

    [Fact]
    public void 空コンテナーのJSON文字列生成には対応しません()
    {
        var objectError = Record.Exception(() => new JsonObject().ToJson());
        var arrayError = Record.Exception(() => new JsonArray().ToJson());

        objectError.Should().NotBeNull();
        arrayError.Should().NotBeNull();
    }

    [Fact]
    public void インデクサーで既存の配列要素とオブジェクトの値を置換します()
    {
#pragma warning disable CA1859 // Exercise the public interface call site for both implementations.
        IJsonValue tested = Json.Parse("""{"a":[1],"b":"before"}""");
#pragma warning restore CA1859
        tested["a"][0] = new JsonLiteral("2", LiteralType.Number);
        tested[1] = new JsonLiteral("after", LiteralType.String);

        tested.Should().BeOfType<JsonObject>().Which.Pairs.Should().HaveCount(2);
        var array = tested["a"].Should().BeOfType<JsonArray>().Which;
        array.Elements.Should().ContainSingle();
        var number = array[0].Should().BeOfType<JsonLiteral>().Which;
        number.ValueType.Should().Be(LiteralType.Number);
        number.Value.Should().Be("2");
        var text = tested["b"].Should().BeOfType<JsonLiteral>().Which;
        text.ValueType.Should().Be(LiteralType.String);
        text.Value.Should().Be("after");
    }

    [Fact]
    public void 数値をdoubleとその配列へマップします()
    {
        Json.Map<double>(new JsonLiteral("12", LiteralType.Number)).Should().Be(12d);
        Json.Map<double[]>(Json.Parse("""{"a":[12,3]}""")["a"]).Should().Equal(12d, 3d);
    }

    [Fact]
    public void 数値をintへ自動変換してマップしません()
    {
        var map = () => Json.Map<int>(new JsonLiteral("12", LiteralType.Number));

        map.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void nullを参照型へマップして返すことも拒否します()
    {
        var map = () => Json.Map<string>(new JsonLiteral("", LiteralType.Null) { Value = null });

        map.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void オブジェクトを公開プロパティとフィールドへマップします()
    {
        const string text = """{"Name":"sample","Count":12,"Marker":"field"}""";
        var mapped = Json.Map<MappingSample>(Json.Parse(text));
        var deserialized = Json.Deserialize<MappingSample>(text);

        mapped.Should().BeEquivalentTo(new MappingSample { Name = "sample", Count = 12, Marker = "field" });
        deserialized.Should().BeEquivalentTo(mapped);
    }

    [Fact]
    public void CLR値とJSON値をシリアライズします()
    {
        double[] numbers = [12d, 3d];

        Json.Serialize("a/b").Should().Be("\"a\\/b\"");
        Json.Serialize(12d).Should().Be("12");
        Json.Serialize(true).Should().Be("true");
        Json.Serialize(false).Should().Be("false");
        Json.Serialize<object?>(null).Should().Be("null");
        Json.Serialize(numbers).Should().Be("[\r\n\t12,\r\n\t3\r\n]");
        Json.Serialize(Json.Parse("""{"a":12}""")).Should().Be("{\r\n\t\"a\": 12\r\n}");
    }

    [Fact]
    public void CLRオブジェクトのプロパティとフィールドをシリアライズします()
    {
        var sample = new MappingSample { Name = "sample", Count = 12, Marker = "field" };

        Json.Serialize(sample).Should().Be(
            "{\r\n\t\"Name\": \"sample\",\r\n\t\"Count\": 12,\r\n\t\"Marker\": \"field\"\r\n}");
    }

    [Fact]
    public void 整数のシリアライズには対応しません()
    {
        var serialize = () => Json.Serialize(12);

        serialize.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ASCIIとBMPを含むネストしたJSONを期待した値へ解析します()
    {
        const string text = """{"ascii":"plain","名前":"éあ","nested":{"values":[12,true,false,null,{"文字":"中"}]}}""";

        var tested = Json.Parse(text);

        tested.Should().BeEquivalentTo(new JsonObject
        {
            Pairs =
            {
                ["ascii"] = new JsonLiteral("plain", LiteralType.String),
                ["名前"] = new JsonLiteral("éあ", LiteralType.String),
                ["nested"] = new JsonObject
                {
                    Pairs =
                    {
                        ["values"] = new JsonArray
                        {
                            Elements =
                            [
                                new JsonLiteral("12", LiteralType.Number),
                                new JsonLiteral("true", LiteralType.Boolean),
                                new JsonLiteral("false", LiteralType.Boolean),
                                new JsonLiteral("", LiteralType.Null) { Value = null },
                                new JsonObject
                                {
                                    Pairs = { ["文字"] = new JsonLiteral("中", LiteralType.String) }
                                }
                            ]
                        }
                    }
                }
            }
        }, it => it.PreferringRuntimeMemberTypes().WithStrictOrdering());
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    public void 空入力と不正オブジェクトはParseExceptionになります(string text)
    {
        var parse = () => Json.Parse(text);

        parse.Should().Throw<ParseException>();
    }

    [Fact]
    public void 空のオブジェクトを識別します()
    {
        var result = Json.Parse("{}");

        result.Pairs.Should().BeEmpty();
    }

    [Fact]
    public void オブジェクトの中身の値を識別します()
    {
        var result = Json.Parse(@"{""a"":1}");

        result.Pairs.Should().ContainSingle();
        var value = result["a"];
        value.Should().BeOfType<JsonLiteral>();
    }
    [Fact]
    public void オブジェクトの中身は複数持てます()
    {
        var result = Json.Parse(@"{""a"":1,""b"":2}");

        result.Pairs.Count.Should().Be(2);
        var value = result["a"];
        value.Should().BeOfType<JsonLiteral>();
        var value2 = result["b"];
        value2.Should().BeOfType<JsonLiteral>();
    }

    [Fact]
    public void 数値はマイナスを識別します()
    {
        var result = Json.Parse(@"{""a"":-1}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("-1");
    }

    [Fact]
    public void 数値は複数桁を識別します()
    {
        var result = Json.Parse(@"{""a"":123}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("123");
    }

    [Fact]
    public void 数値は0から9を識別します()
    {
        var result = Json.Parse(@"{""a"":1234567890}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1234567890");
    }

    [Fact]
    public void 数値は小数を識別します()
    {
        var result = Json.Parse(@"{""a"":1.2}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1.2");
    }

    [Fact]
    public void 数値は小数点以下複数桁を識別します()
    {
        var result = Json.Parse(@"{""a"":1.234}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1.234");
    }

    [Fact]
    public void 整数部の数字がない小数を受理します()
    {
        ((JsonLiteral)Json.Parse(@"{""a"":.1}")["a"]).Value.Should().Be(".1");
    }

    [Fact]
    public void 小数部の数字がなくても受理します()
    {
        ((JsonLiteral)Json.Parse(@"{""a"":0.}")["a"]).Value.Should().Be("0.");
    }

    [Fact]
    public void 数値は指数部を識別します()
    {
        var result = Json.Parse(@"{""a"":1e0}");
        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e0");
    }

    [Fact]
    public void 数値の指数部Eは大文字でも小文字でも識別します()
    {
        var lower = (JsonLiteral)Json.Parse(@"{""a"":1e0}")["a"];
        var upper = (JsonLiteral)Json.Parse(@"{""a"":1E0}")["a"];

        lower.Value.Should().Be("1e0");
        upper.Value.Should().Be("1E0");
    }

    [Fact]
    public void 数値の指数部のプラスを識別します()
    {
        var result = Json.Parse(@"{""a"":1e+0}");
        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e+0");
    }

    [Fact]
    public void 数値の指数部のマイナスを識別します()
    {
        var result = Json.Parse(@"{""a"":1e-0}");
        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e-0");
    }
    [Fact]
    public void 数値の指数部の複数の数字を識別します()
    {
        var result = Json.Parse(@"{""a"":1e+10}");
        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e+10");
    }

    [Fact]
    public void 数値の指数部のプラスマイナスが数字の後だと認識しません()
    {
        Assert.Throws<ParseException>(
            () => Json.Parse(@"{""a"":e10+}"));
    }
    [Fact]
    public void 数値の指数部のプラスマイナスがeの前だと認識しません()
    {
        Assert.Throws<ParseException>(
            () => Json.Parse(@"{""a"":+e10}"));
    }

    [Fact]
    public void 値の省略を空の数値として読み込みます()
    {
        var tested = (JsonLiteral)Json.Parse(@"{""a"":}")["a"];

        tested.ValueType.Should().Be(LiteralType.Number);
        tested.Value.Should().BeEmpty();
    }

    [Theory]
    [InlineData("{\"key")]
    [InlineData("{\"key\":\"value")]
    public void 閉じ引用符のないキーや文字列値はParseExceptionになります(string text)
    {
        var parse = () => Json.Parse(text);

        parse.Should().Throw<ParseException>();
    }

    [Fact]
    public void 空文字列のキーを拒否します()
    {
        var parse = () => Json.Parse("""{"":""}""");

        parse.Should().Throw<ParseException>();
    }

    [Fact]
    public void 文字列の値を識別します()
    {
        var result = Json.Parse(@"{""a"":""b""}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.String);
        value.Value.Should().Be("b");
    }

    [Fact]
    public void 文字列の複数文字を識別します()
    {
        var result = Json.Parse(@"{""a"":""bc""}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.String);
        value.Value.Should().Be("bc");
    }

    [Fact]
    public void 文字列のエスケープ文字を識別します()
    {
        var result = Json.Parse("{\"a\":\"\\\\\\\"\\b\\f\\n\\r\\t\"}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.String);
        value.Value.Should().Be("\\\"\b\f\n\r\t");
    }
    [Fact]
    public void 真偽値の値を識別します()
    {
        var result = Json.Parse(@"{""a"":true}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Boolean);
        value.Value.Should().Be("true");
    }

    [Fact]
    public void 真偽値はtrueとfalseが可能です()
    {
        var result = Json.Parse(@"{""a"":true,""b"":false}");

        result.Pairs.Count.Should().Be(2);
        var trueValue = (JsonLiteral)result["a"];
        trueValue.ValueType.Should().Be(LiteralType.Boolean);
        trueValue.Value.Should().Be("true");
        var falseValue = (JsonLiteral)result["b"];
        falseValue.ValueType.Should().Be(LiteralType.Boolean);
        falseValue.Value.Should().Be("false");
    }

    [Fact]
    public void 真偽値は大文字と小文字を区別せずに識別します()
    {
        var result = Json.Parse(@"{""a"":TRUE,""b"":FALSE}");

        result.Pairs.Count.Should().Be(2);
        var trueValue = (JsonLiteral)result["a"];
        trueValue.ValueType.Should().Be(LiteralType.Boolean);
        trueValue.Value.Should().Be("TRUE");
        var falseValue = (JsonLiteral)result["b"];
        falseValue.ValueType.Should().Be(LiteralType.Boolean);
        falseValue.Value.Should().Be("FALSE");
    }
    [Fact]
    public void Nullの値を識別します()
    {
        var result = Json.Parse(@"{""a"":null}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Null);
        value.Value.Should().BeNull();
    }

    [Fact]
    public void Nullの値の識別は大文字小文字を区別しません()
    {
        var result = Json.Parse(@"{""a"":NULL}");

        result.Pairs.Should().ContainSingle();
        var value = (JsonLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Null);
        value.Value.Should().BeNull();
    }
    [Fact]
    public void オブジェクトの中身の配列を識別します()
    {
        var result = Json.Parse(@"{""a"":[]}");

        result.Pairs.Should().ContainSingle();
        var value = result["a"];
        value.Should().BeOfType<JsonArray>();
    }

    [Theory]
    [InlineData("""{"a":[]}""")]
    [InlineData("""{"a":[ ]}""")]
    public void 空の配列を空の数値一要素として読み込みます(string text)
    {
        var array = (JsonArray)Json.Parse(text)["a"];

        array.Elements.Should().ContainSingle();
        ((JsonLiteral)array[0]).ValueType.Should().Be(LiteralType.Number);
        ((JsonLiteral)array[0]).Value.Should().BeEmpty();
    }

    [Fact]
    public void オブジェクトの中身の配列の中身を識別します()
    {
        var result = Json.Parse(@"{""a"":[1]}");

        result.Pairs.Should().ContainSingle();
        var array = (JsonArray)result["a"];
        array.Elements.Should().ContainSingle();
        array.Elements[0].Should().BeOfType<JsonLiteral>();
    }
    [Fact]
    public void 配列は複数の要素を持ちます()
    {
        var result = Json.Parse(@"{""a"":[1,2]}");

        result.Pairs.Should().ContainSingle();
        var array = (JsonArray)result["a"];
        array.Elements.Count.Should().Be(2);
        array.Elements[0].Should().BeOfType<JsonLiteral>();
        array.Elements[1].Should().BeOfType<JsonLiteral>();
    }
    [Fact]
    public void オブジェクトの中身のオブジェクトを識別します()
    {
        var result = Json.Parse(@"{""a"":{}}");

        result.Pairs.Should().ContainSingle();
        var value = result["a"];
        value.Should().BeOfType<JsonObject>();
    }

    [Fact]
    public void オブジェクトの前に空白があっても読み込みます()
        => Json.Parse(@" {}");

    [Fact]
    public void タブ記号も空白として読み飛ばします()
        => Json.Parse("\t{}");

    [Fact]
    public void CR記号も空白として読み飛ばします()
        => Json.Parse("\r{}");

    [Fact]
    public void LF記号も空白として読み飛ばします()
        => Json.Parse("\n{}");

    [Fact]
    public void 四種類の空白が混在しても構造と値を同じように読み込みます()
    {
        const string whitespace = " \t\r\n";
        var text = $$"""
            {{whitespace}}{
                {{whitespace}}"a"{{whitespace}}:{{whitespace}}[
                    {{whitespace}}"b"{{whitespace}},{{whitespace}}12{{whitespace}},
                    {{whitespace}}true{{whitespace}},{{whitespace}}null{{whitespace}}
                ]{{whitespace}}
            }
            """;

        Json.Parse(text).Should().BeEquivalentTo(
            Json.Parse("""{"a":["b",12,true,null]}"""),
            it => it.PreferringRuntimeMemberTypes().WithStrictOrdering());
    }

    [Theory]
    [InlineData('\v')]
    [InlineData('\f')]
    [InlineData('\u00a0')]
    public void Unicodeの空白を読み飛ばします(char character)
    {
        Json.Parse($"{character}{{}}").Pairs.Should().BeEmpty();
    }

    [Fact]
    public void NUL文字を空白として読み飛ばしません()
    {
        var parse = () => Json.Parse("\0{}");

        parse.Should().Throw<ParseException>();
    }

    [Fact]
    public void 空白だけの入力はParseExceptionになります()
    {
        var parse = () => Json.Parse(" \t\r\n");

        parse.Should().Throw<ParseException>();
    }

    [Fact]
    public void オブジェクト終了の前に空白があっても読み込みます()
        => Json.Parse(@"{ }");
    [Fact]
    public void 配列の前に空白があっても読み込みます()
        => Json.Parse(@"{""a"": []}");
    [Fact]
    public void オブジェクト区切りのコロンの前に空白があっても読み込みます()
        => Json.Parse(@"{""a"" :1}");

    [Fact]
    public void オブジェクト区切りのカンマの前に空白があっても読み込みます()
        => Json.Parse(@"{""a"":1 ,""b"":1}");

    [Fact]
    public void 配列区切りのカンマの前に空白があっても読み込みます()
        => Json.Parse(@"{""a"":[1 ,2]}");

    [Fact]
    public void Nullの前に空白があっても読み込みます()
        => Json.Parse(@"{""a"": null}");
    [Fact]
    public void 真偽値の前に空白があっても読み込みます()
        => Json.Parse(@"{""a"": true}");

    [Fact]
    public void 数値の前に空白があっても読み込みます()
        => Json.Parse(@"{""a"": 1}");

    [Fact]
    public void 符号と数字の間に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => Json.Parse(@"{""a"":- 1}"));
    }

    [Fact]
    public void 数字と数字の間に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => Json.Parse(@"{""a"":1 0}"));
    }

    [Fact]
    public void 小数点の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => Json.Parse(@"{""a"":1 .}"));
    }

    [Fact]
    public void 少数部の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => Json.Parse(@"{""a"":1. 0}"));
    }

    [Fact]
    public void 指数部の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => Json.Parse(@"{""a"":1.0 e}"));
    }

    [Fact]
    public void 指数部の符号の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => Json.Parse(@"{""a"":1.0e +}"));
    }

    [Fact]
    public void 指数部の数字の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => Json.Parse(@"{""a"":1.0e+ 1}"));
    }

    [Fact]
    public void 文字列の前に空白があっても読み込みます()
        => Json.Parse(@"{""a"": ""b""}");

    [Fact]
    public void 文字列の中の空白は文字列として読み込みます()
    {
        var result = Json.Parse(@"{""a"":"" b ""}");

        result.Pairs.Should().ContainSingle();
        var str = (JsonLiteral)result["a"];
        str.Value.Should().Be(@" b ");
    }
}
