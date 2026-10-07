using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class RecursiveParserのテスト
{
    [Fact]
    public void ParseAsyncは内部パーサーと同じように成功します()
    {
        var cursol = Cursol.FromString("public");
        var parser = new CharParser('p');
        var tested = new RecursiveParser<char>(() => parser);
        var result = tested.Parse(cursol);
        var expected = parser.Parse(cursol);
        result.IsSuccess.Should().Be(expected.IsSuccess);
        (result.Cursol == expected.Cursol).Should().BeTrue();
        result.Parsed.Should().Be(expected.Parsed);
    }
    [Fact]
    public void ParseAsyncは内部パーサーと同じように失敗します()
    {
        var cursol = Cursol.FromString("public");
        var parser = new CharParser('a');
        var tested = new RecursiveParser<char>(() => parser);
        var result = tested.Parse(cursol);
        var expected = parser.Parse(cursol);
        result.IsSuccess.Should().Be(expected.IsSuccess);
        (result.Cursol == expected.Cursol).Should().BeTrue();
        var readParsed = () => tested.Parse(Cursol.FromString("public")).Parsed;
        var readExpected = () => parser.Parse(Cursol.FromString("public")).Parsed;
        readParsed.Should().Throw<InvalidOperationException>();
        readExpected.Should().Throw<InvalidOperationException>();
    }
}
