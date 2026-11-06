using FluentAssertions;
using Marimo.Kotowari.Core;

using System;

using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class WithWhiteSpaceParserのテスト
{
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
