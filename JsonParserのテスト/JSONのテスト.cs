using FluentAssertions;
using Marimo.Parser;
using Marimo.Kotowari;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Parser.Test;

public class JSONのテスト
{
    [Fact]
    public void 空のオブジェクトを識別します()
    {
        var result =JSON.Parse("{}");

        result.Pairs.Should().BeEmpty();
    }

    [Fact]
    public void オブジェクトの中身の値を識別します()
    {
        var result = JSON.Parse(@"{""a"":1}");

        result.Pairs.Should().ContainSingle();
        var value = result["a"];
        value.Should().BeOfType<JSONLiteral>();
    }
    [Fact]
    public void オブジェクトの中身は複数持てます()
    {
        var result = JSON.Parse(@"{""a"":1,""b"":2}");

        result.Pairs.Count.Should().Be(2);
        var value = result["a"];
        value.Should().BeOfType<JSONLiteral>();
        var value2 = result["b"];
        value2.Should().BeOfType<JSONLiteral>();
    }

    [Fact]
    public void 数値はマイナスを識別します()
    {
        var result = JSON.Parse(@"{""a"":-1}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("-1");
    }

    [Fact]
    public void 数値は複数桁を識別します()
    {
        var result = JSON.Parse(@"{""a"":123}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("123");
    }

    [Fact]
    public void 数値は0から9を識別します()
    {
        var result = JSON.Parse(@"{""a"":1234567890}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1234567890");
    }

    [Fact]
    public void 数値は小数を識別します()
    {
        var result = JSON.Parse(@"{""a"":1.2}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1.2");
    }

    [Fact]
    public void 数値は小数点以下複数桁を識別します()
    {
        var result = JSON.Parse(@"{""a"":1.234}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1.234");
    }

    [Fact]
    public void 数値は小数で整数部の数字が必要です()
    {
        var parse = () => JSON.Parse(@"{""a"":.1}");

        parse.Should().Throw<ParseException>();
    }

    [Fact]
    public void 数値は小数で小数部の数字が必要です()
    {
        var parse = () => JSON.Parse(@"{""a"":0.}");

        parse.Should().Throw<ParseException>();
    }

    [Fact]
    public void 数値は指数部を識別します()
    {
        var result = JSON.Parse(@"{""a"":1e0}");
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e0");
    }

    [Fact]
    public void 数値の指数部Eは大文字でも小文字でも識別します()
    {
        var lower = (JSONLiteral)JSON.Parse(@"{""a"":1e0}")["a"];
        var upper = (JSONLiteral)JSON.Parse(@"{""a"":1E0}")["a"];

        lower.Value.Should().Be("1e0");
        upper.Value.Should().Be("1E0");
    }

    [Fact]
    public void 数値の指数部のプラスを識別します()
    {
        var result = JSON.Parse(@"{""a"":1e+0}");
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e+0");
    }

    [Fact]
    public void 数値の指数部のマイナスを識別します()
    {
        var result = JSON.Parse(@"{""a"":1e-0}");
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e-0");
    }
    [Fact]
    public void 数値の指数部の数値を識別します()
    {
        var result = JSON.Parse(@"{""a"":1e0}");
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e0");
    }
    [Fact]
    public void 数値の指数部の複数の数字を識別します()
    {
        var result = JSON.Parse(@"{""a"":1e+10}");
        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Number);
        value.Value.Should().Be("1e+10");
    }

    [Fact]
    public void 数値の指数部のプラスマイナスが数字の後だと認識しません()
    {
        Assert.Throws<ParseException>(
            () => JSON.Parse(@"{""a"":e10+}"));
    }
    [Fact]
    public void 数値の指数部のプラスマイナスがeの前だと認識しません()
    {
        Assert.Throws<ParseException>(
            () => JSON.Parse(@"{""a"":+e10}"));
    }

    [Fact]
    public void 値が何もない場合は読み込みません()
    {
        var parse = () => JSON.Parse(@"{""a"":}");

        parse.Should().Throw<ParseException>();
    }

    [Fact]
    public void 文字列の値を識別します()
    {
        var result = JSON.Parse(@"{""a"":""b""}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.String);
        value.Value.Should().Be("b");
    }

    [Fact]
    public void 文字列の複数文字を識別します()
    {
        var result = JSON.Parse(@"{""a"":""bc""}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.String);
        value.Value.Should().Be("bc");
    }

    [Fact]
    public void 文字列のエスケープ文字を識別します()
    {
        var result = JSON.Parse("{\"a\":\"\\\\\\\"\\b\\f\\n\\r\\t\"}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.String);
        value.Value.Should().Be("\\\"\b\f\n\r\t");
    }
    [Fact]
    public void 真偽値の値を識別します()
    {
        var result = JSON.Parse(@"{""a"":true}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Boolean);
        value.Value.Should().Be("true");
    }

    [Fact]
    public void 真偽値はtrueとfalseが可能です()
    {
        var result = JSON.Parse(@"{""a"":true,""b"":false}");

        result.Pairs.Count.Should().Be(2);
        var trueValue = (JSONLiteral)result["a"];
        trueValue.ValueType.Should().Be(LiteralType.Boolean);
        trueValue.Value.Should().Be("true");
        var falseValue = (JSONLiteral)result["b"];
        falseValue.ValueType.Should().Be(LiteralType.Boolean);
        falseValue.Value.Should().Be("false");
    }

    [Fact]
    public void 真偽値は大文字と小文字を区別せずに識別します()
    {
        var result = JSON.Parse(@"{""a"":TRUE,""b"":FALSE}");

        result.Pairs.Count.Should().Be(2);
        var trueValue = (JSONLiteral)result["a"];
        trueValue.ValueType.Should().Be(LiteralType.Boolean);
        trueValue.Value.Should().Be("TRUE");
        var falseValue = (JSONLiteral)result["b"];
        falseValue.ValueType.Should().Be(LiteralType.Boolean);
        falseValue.Value.Should().Be("FALSE");
    }
    [Fact]
    public void Nullの値を識別します()
    {
        var result = JSON.Parse(@"{""a"":null}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Null);
        value.Value.Should().BeNull();
    }

    [Fact]
    public void Nullの値の識別は大文字小文字を区別しません()
    {
        var result = JSON.Parse(@"{""a"":NULL}");

        result.Pairs.Should().ContainSingle();
        var value = (JSONLiteral)result["a"];
        value.ValueType.Should().Be(LiteralType.Null);
        value.Value.Should().BeNull();
    }
    [Fact]
    public void オブジェクトの中身の配列を識別します()
    {
        var result = JSON.Parse(@"{""a"":[]}");

        result.Pairs.Should().ContainSingle();
        var value = result["a"];
        value.Should().BeOfType<JSONArray>();
    }

    [Fact]
    public void 空の配列に要素はありません()
    {
        var array = (JSONArray)JSON.Parse(@"{""a"":[]}")["a"];

        array.Elements.Should().BeEmpty();
    }

    [Fact]
    public void オブジェクトの中身の配列の中身を識別します()
    {
        var result = JSON.Parse(@"{""a"":[1]}");

        result.Pairs.Should().ContainSingle();
        var array = (JSONArray)result["a"];
        array.Elements.Should().ContainSingle();
        array.Elements[0].Should().BeOfType<JSONLiteral>();
    }
    [Fact]
    public void 配列は複数の要素を持ちます()
    {
        var result = JSON.Parse(@"{""a"":[1,2]}");

        result.Pairs.Should().ContainSingle();
        var array = (JSONArray)result["a"];
        array.Elements.Count.Should().Be(2);
        array.Elements[0].Should().BeOfType<JSONLiteral>();
        array.Elements[1].Should().BeOfType<JSONLiteral>();
    }
    [Fact]
    public void オブジェクトの中身のオブジェクトを識別します()
    {
        var result = JSON.Parse(@"{""a"":{}}");

        result.Pairs.Should().ContainSingle();
        var value = result["a"];
        value.Should().BeOfType<JSONObject>();
    }

    [Fact]
    public void オブジェクトの前に空白があっても読み込みます()
        => JSON.Parse(@" {}");

    [Fact]
    public void タブ記号も空白として読み飛ばします()
        => JSON.Parse("\t{}");

    [Fact]
    public void CR記号も空白として読み飛ばします()
        => JSON.Parse("\r{}");

    [Fact]
    public void LF記号も空白として読み飛ばします()
        => JSON.Parse("\n{}");

    [Fact]
    public void オブジェクト終了の前に空白があっても読み込みます()
        => JSON.Parse(@"{ }");
    [Fact]
    public void 配列の前に空白があっても読み込みます()
        => JSON.Parse(@"{""a"": []}");
    [Fact]
    public void 配列終了の前に空白があっても読み込みます()
        => JSON.Parse(@"{""a"":[ ]}");
    [Fact]
    public void オブジェクト区切りのコロンの前に空白があっても読み込みます()
        => JSON.Parse(@"{""a"" :1}");

    [Fact]
    public void オブジェクト区切りのカンマの前に空白があっても読み込みます()
        => JSON.Parse(@"{""a"":1 ,""b"":1}");

    [Fact]
    public void 配列区切りのカンマの前に空白があっても読み込みます()
        => JSON.Parse(@"{""a"":[1 ,2]}");

    [Fact]
    public void Nullの前に空白があっても読み込みます()
        => JSON.Parse(@"{""a"": null}");
    [Fact]
    public void 真偽値の前に空白があっても読み込みます()
        => JSON.Parse(@"{""a"": true}");

    [Fact]
    public void 数値の前に空白があっても読み込みます()
        => JSON.Parse(@"{""a"": 1}");

    [Fact]
    public void 符号と数字の間に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => JSON.Parse(@"{""a"":- 1}"));
    }

    [Fact]
    public void 数字と数字の間に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => JSON.Parse(@"{""a"":1 0}"));
    }

    [Fact]
    public void 小数点の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => JSON.Parse(@"{""a"":1 .}"));
    }

    [Fact]
    public void 少数部の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => JSON.Parse(@"{""a"":1. 0}"));
    }

    [Fact]
    public void 指数部の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => JSON.Parse(@"{""a"":1.0 e}"));
    }

    [Fact]
    public void 指数部の符号の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => JSON.Parse(@"{""a"":1.0e +}"));
    }

    [Fact]
    public void 指数部の数字の前に空白があったら読み込みません()
    {
        Assert.Throws<ParseException>(
            () => JSON.Parse(@"{""a"":1.0e+ 1}"));
    }

    [Fact]
    public void 文字列の前に空白があっても読み込みます()
        => JSON.Parse(@"{""a"": ""b""}");

    [Fact]
    public void 文字列の中の空白は文字列として読み込みます()
    {
        var result = JSON.Parse(@"{""a"":"" b ""}");

        result.Pairs.Should().ContainSingle();
        var str = (JSONLiteral)result["a"];
        str.Value.Should().Be(@" b ");
    }
}
