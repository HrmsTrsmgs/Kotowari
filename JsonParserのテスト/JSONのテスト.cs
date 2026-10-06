using FluentAssertions;
using Marimo.Parser;
using Marimo.Kotowari;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Parser.Test;

public class JSONのテスト
{
    public enum 入力形式
    {
        文字列,
        UTF8
    }

    static JSONObject Parse(string text, 入力形式 inputFormat)
        => inputFormat switch
        {
            入力形式.文字列 => JSON.Parse(text),
            入力形式.UTF8 => JSON.Parse((ReadOnlySpan<byte>)Encoding.UTF8.GetBytes(text)),
            _ => throw new ArgumentOutOfRangeException(nameof(inputFormat))
        };

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void ASCIIとBMPを含むネストしたJSONを期待した値へ解析します(入力形式 inputFormat)
    {
        const string text = """{"ascii":"plain","名前":"éあ","nested":{"values":[12,true,false,null,{"文字":"中"}]}}""";

        var tested = Parse(text, inputFormat);

        tested.Should().BeEquivalentTo(new JSONObject
        {
            Pairs =
            {
                ["ascii"] = new JSONLiteral("plain", LiteralType.String),
                ["名前"] = new JSONLiteral("éあ", LiteralType.String),
                ["nested"] = new JSONObject
                {
                    Pairs =
                    {
                        ["values"] = new JSONArray
                        {
                            Elements =
                            [
                                new JSONLiteral("12", LiteralType.Number),
                                new JSONLiteral("true", LiteralType.Boolean),
                                new JSONLiteral("false", LiteralType.Boolean),
                                new JSONLiteral(LiteralType.Null),
                                new JSONObject
                                {
                                    Pairs = { ["文字"] = new JSONLiteral("中", LiteralType.String) }
                                }
                            ]
                        }
                    }
                }
            }
        });
    }

    [Fact]
    public void UTF8はReadOnlySpanで指定した範囲だけを解析します()
    {
        var bytes = Encoding.UTF8.GetBytes("!{\"名\":\"é\"}?");
        ReadOnlySpan<byte> utf8 = bytes.AsSpan(1, bytes.Length - 2);

        var tested = JSON.Parse(utf8);

        tested.Should().BeEquivalentTo(new JSONObject
        {
            Pairs = { ["名"] = new JSONLiteral("é", LiteralType.String) }
        });
    }

    [Theory]
    [InlineData(入力形式.文字列, "")]
    [InlineData(入力形式.UTF8, "")]
    [InlineData(入力形式.文字列, "{")]
    [InlineData(入力形式.UTF8, "{")]
    public void 空入力と不正オブジェクトはParseExceptionになります(入力形式 inputFormat, string text)
    {
        var parse = () => Parse(text, inputFormat);

        parse.Should().Throw<ParseException>();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 空のオブジェクトを識別します(入力形式 inputFormat)
    {
        var result = Parse("{}", inputFormat);

        result.Pairs.Should().BeEmpty();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void オブジェクトの中身の値を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = result["a"];
        value.Should().BeOfType<JSONLiteral>();
    }
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void オブジェクトの中身は複数持てます(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1,""b"":2}", inputFormat);

        result.Pairs.Count.Should().Be(2);
        var value = result["a"];
        value.Should().BeOfType<JSONLiteral>();
        var value2 = result["b"];
        value2.Should().BeOfType<JSONLiteral>();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値はマイナスを識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":-1}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("-1");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値は複数桁を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":123}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("123");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値は0から9を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1234567890}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1234567890");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値は小数を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1.2}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1.2");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値は小数点以下複数桁を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1.234}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1.234");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値は小数で整数部の数字が必要です(入力形式 inputFormat)
    {
        var parse = () => Parse(@"{""a"":.1}", inputFormat);

        parse.Should().Throw<ParseException>();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値は小数で小数部の数字が必要です(入力形式 inputFormat)
    {
        var parse = () => Parse(@"{""a"":0.}", inputFormat);

        parse.Should().Throw<ParseException>();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値は指数部を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1e0}", inputFormat);
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e0");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値の指数部Eは大文字でも小文字でも識別します(入力形式 inputFormat)
    {
        var lower = (JSONLiteral)Parse(@"{""a"":1e0}", inputFormat)["a"];
        var upper = (JSONLiteral)Parse(@"{""a"":1E0}", inputFormat)["a"];

        lower.Value.Should().Be("1e0");
        upper.Value.Should().Be("1E0");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値の指数部のプラスを識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1e+0}", inputFormat);
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e+0");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値の指数部のマイナスを識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1e-0}", inputFormat);
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e-0");
    }
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値の指数部の数値を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1e0}", inputFormat);
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e0");
    }
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値の指数部の複数の数字を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":1e+10}", inputFormat);
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e+10");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値の指数部のプラスマイナスが数字の後だと認識しません(入力形式 inputFormat)
    {
        Assert.Throws<ParseException>(
            () => Parse(@"{""a"":e10+}", inputFormat));
    }
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値の指数部のプラスマイナスがeの前だと認識しません(入力形式 inputFormat)
    {
        Assert.Throws<ParseException>(
            () => Parse(@"{""a"":+e10}", inputFormat));
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 値が何もない場合は読み込みません(入力形式 inputFormat)
    {
        var parse = () => Parse(@"{""a"":}", inputFormat);

        parse.Should().Throw<ParseException>();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 文字列の値を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":""b""}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.String);
        value.Value.Should().Be("b");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 文字列の複数文字を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":""bc""}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.String);
        value.Value.Should().Be("bc");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 文字列のエスケープ文字を識別します(入力形式 inputFormat)
    {
        var result = Parse("{\"a\":\"\\\\\\\"\\b\\f\\n\\r\\t\"}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.String);
        value.Value.Should().Be("\\\"\b\f\n\r\t");
    }
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 真偽値の値を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":true}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Boolean);
        value.Value.Should().Be("true");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 真偽値はtrueとfalseが可能です(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":true,""b"":false}", inputFormat);

        result.Pairs.Count.Should().Be(2);
        var trueValue = (JSONLiteral)result["a"];
        trueValue.ValueType.Should().Be(LiteralType.Boolean);
        trueValue.Value.Should().Be("true");
        var falseValue = (JSONLiteral)result["b"];
        falseValue.ValueType.Should().Be(LiteralType.Boolean);
        falseValue.Value.Should().Be("false");
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 真偽値は大文字と小文字を区別せずに識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":TRUE,""b"":FALSE}", inputFormat);

        result.Pairs.Count.Should().Be(2);
        var trueValue = (JSONLiteral)result["a"];
        trueValue.ValueType.Should().Be(LiteralType.Boolean);
        trueValue.Value.Should().Be("TRUE");
        var falseValue = (JSONLiteral)result["b"];
        falseValue.ValueType.Should().Be(LiteralType.Boolean);
        falseValue.Value.Should().Be("FALSE");
    }
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void Nullの値を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":null}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Null);
        value.Value.Should().BeNull();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void Nullの値の識別は大文字小文字を区別しません(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":NULL}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Null);
        value.Value.Should().BeNull();
    }
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void オブジェクトの中身の配列を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":[]}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = result["a"];
        value.Should().BeOfType<JSONArray>();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 空の配列に要素はありません(入力形式 inputFormat)
    {
        var array = (JSONArray)Parse(@"{""a"":[]}", inputFormat)["a"];

        array.Elements.Should().BeEmpty();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void オブジェクトの中身の配列の中身を識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":[1]}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var array = (JSONArray)result["a"];
        array.Elements.Should().ContainSingle();
        array.Elements[0].Should().BeOfType<JSONLiteral>();
    }
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 配列は複数の要素を持ちます(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":[1,2]}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var array = (JSONArray)result["a"];
        array.Elements.Count.Should().Be(2);
        array.Elements[0].Should().BeOfType<JSONLiteral>();
        array.Elements[1].Should().BeOfType<JSONLiteral>();
    }
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void オブジェクトの中身のオブジェクトを識別します(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":{}}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var value = result["a"];
        value.Should().BeOfType<JSONObject>();
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void オブジェクトの前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@" {}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void タブ記号も空白として読み飛ばします(入力形式 inputFormat)
        => Parse("\t{}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void CR記号も空白として読み飛ばします(入力形式 inputFormat)
        => Parse("\r{}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void LF記号も空白として読み飛ばします(入力形式 inputFormat)
        => Parse("\n{}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void オブジェクト終了の前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{ }", inputFormat);
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 配列の前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{""a"": []}", inputFormat);
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 配列終了の前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{""a"":[ ]}", inputFormat);
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void オブジェクト区切りのコロンの前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{""a"" :1}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void オブジェクト区切りのカンマの前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{""a"":1 ,""b"":1}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 配列区切りのカンマの前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{""a"":[1 ,2]}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void Nullの前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{""a"": null}", inputFormat);
    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 真偽値の前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{""a"": true}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数値の前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{""a"": 1}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 符号と数字の間に空白があったら読み込みません(入力形式 inputFormat)
    {
        Assert.Throws<ParseException>(
            () => Parse(@"{""a"":- 1}", inputFormat));
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 数字と数字の間に空白があったら読み込みません(入力形式 inputFormat)
    {
        Assert.Throws<ParseException>(
            () => Parse(@"{""a"":1 0}", inputFormat));
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 小数点の前に空白があったら読み込みません(入力形式 inputFormat)
    {
        Assert.Throws<ParseException>(
            () => Parse(@"{""a"":1 .}", inputFormat));
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 少数部の前に空白があったら読み込みません(入力形式 inputFormat)
    {
        Assert.Throws<ParseException>(
            () => Parse(@"{""a"":1. 0}", inputFormat));
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 指数部の前に空白があったら読み込みません(入力形式 inputFormat)
    {
        Assert.Throws<ParseException>(
            () => Parse(@"{""a"":1.0 e}", inputFormat));
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 指数部の符号の前に空白があったら読み込みません(入力形式 inputFormat)
    {
        Assert.Throws<ParseException>(
            () => Parse(@"{""a"":1.0e +}", inputFormat));
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 指数部の数字の前に空白があったら読み込みません(入力形式 inputFormat)
    {
        Assert.Throws<ParseException>(
            () => Parse(@"{""a"":1.0e+ 1}", inputFormat));
    }

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 文字列の前に空白があっても読み込みます(入力形式 inputFormat)
        => Parse(@"{""a"": ""b""}", inputFormat);

    [Theory]
    [InlineData(入力形式.文字列)]
    [InlineData(入力形式.UTF8)]
    public void 文字列の中の空白は文字列として読み込みます(入力形式 inputFormat)
    {
        var result = Parse(@"{""a"":"" b ""}", inputFormat);

        result.Pairs.Should().ContainSingle();
        var str = (JSONLiteral)result["a"];
        str.Value.Should().Be(@" b ");
    }
}
