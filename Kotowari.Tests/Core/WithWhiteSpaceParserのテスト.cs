using FluentAssertions;
using Marimo.Kotowari.Core;

using System;

using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class WithWhiteSpaceParserのテスト
{
    [Fact]
    public void 子のnull成功値を保持します()
    {
        var tested = new WithWhiteSpaceParser<string?>(
            new CharParser('_'),
            ParserConverter.Create<string?, char>(new CharParser('a'), _ => null));

        var result = tested.Parse(new Cursol("__a!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().BeNull();
        result.Cursol.Index.Should().Be(3);
    }

    [Fact]
    public void 空白が入力を消費せず成功したら例外を投げます()
    {
        var attempts = 0;
        var tested = new WithWhiteSpaceParser<char>(
            new CallbackCharParser((_, _) =>
                ++attempts == 1
                    ? (true, ' ', 0)
                    : throw new NotSupportedException("反復が停止しませんでした")),
            new CharParser('a'));
        var parse = () => tested.Parse(new Cursol("a"));

        parse.Should().Throw<InvalidOperationException>();
    }

}
