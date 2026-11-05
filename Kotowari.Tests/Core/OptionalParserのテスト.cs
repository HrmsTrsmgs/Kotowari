using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class OptionalParserのテスト
{
    [Fact]
    public void 子が成功したら値が存在し子が消費した位置で成功します()
    {
        var tested = new OptionalParser<char>(new CharParser('-'));

        var result = tested.Parse(new Cursol("-1"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.IsPresent.Should().BeTrue();
        result.Parsed.Value.Should().Be('-');
        result.Cursol.Index.Should().Be(1);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("")]
    public void 子が入力を消費せず失敗したら不在の値で成功しカーソルは進みません(string text)
    {
        var tested = new OptionalParser<char>(new CharParser('-'));

        var result = tested.Parse(new Cursol(text));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.IsPresent.Should().BeFalse();
        result.Cursol.Index.Should().Be(0);
    }

    [Fact]
    public void 子が入力を消費して失敗したら不在の値で成功し呼び出し開始位置に戻ります()
    {
        var cursol = new Cursol("x b").GoFoward(1);
        var tested = new OptionalParser<char>(
            new WithWhiteSpaceParser<char>(new CharParser(' '), new CharParser('a')));

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
        result.Parsed.IsPresent.Should().BeFalse();
        result.Cursol.Index.Should().Be(cursol.Index);
    }

    [Fact]
    public void 子の例外は不在とせずそのまま伝播します()
    {
        var exception = new InvalidOperationException();
        var tested = new OptionalParser<char>(new CallbackCharParser((_, _) => throw exception));

        var parse = () => tested.Parse(new Cursol("-"));

        parse.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
    }
}
