using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class DebugParserのテスト
{
    [Fact]
    public void 子のnull成功値を返します()
    {
        var tested = new DebugParser<string?>(
            ParserConverter.Create<string?, char>(new CharParser('a'), _ => null), () => { });

        var result = tested.Parse(new Cursol("a!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().BeNull();
        result.Cursol.Index.Should().Be(1);
    }

    [Fact]
    public void 内部パーサーと同じように成功します()
    {
        var cursol = new Cursol("public");
        var parser = new CharParser('p');
        var tested = new DebugParser<char>(parser, () => { });
        var result = tested.Parse(cursol);
        var expected = parser.Parse(cursol);
        result.IsSuccess.Should().Be(expected.IsSuccess);
        (result.Cursol == expected.Cursol).Should().BeTrue();
        result.Parsed.Should().Be(expected.Parsed);
    }
    [Fact]
    public void 内部パーサーと同じように失敗します()
    {
        var cursol = new Cursol("public");
        var parser = new CharParser('a');
        var tested = new DebugParser<char>(parser, () => { });
        var result = tested.Parse(cursol);
        var expected = parser.Parse(cursol);
        result.IsSuccess.Should().Be(expected.IsSuccess);
        (result.Cursol == expected.Cursol).Should().BeTrue();
        var readParsed = () => tested.Parse(new Cursol("public")).Parsed;
        var readExpected = () => parser.Parse(new Cursol("public")).Parsed;
        readParsed.Should().Throw<InvalidOperationException>();
        readExpected.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void 指定したアクションを呼びます()
    {
        bool isActioned = false;
        var cursol = new Cursol("public");
        var parser = new CharParser('a');
        var tested = new DebugParser<char>(parser, () => isActioned = true);

        isActioned.Should().BeFalse();
        tested.Parse(cursol);
        isActioned.Should().BeTrue();
    }
}
