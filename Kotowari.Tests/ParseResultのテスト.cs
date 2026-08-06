using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using Xunit;

namespace Marimo.Kotowari.Tests;

public class ParseResultのテスト
{
    [Fact]
    public void 成功結果は値とカーソルを保持します()
    {
        var cursol = new Cursol("text");

        var result = ParseResult<string>.Success(cursol, "parsed");

        result.IsSuccess.Should().BeTrue();
        result.Cursol.Should().Be(cursol);
        result.Parsed.Should().Be("parsed");
    }

    [Fact]
    public void 失敗結果はカーソルを保持します()
    {
        var cursol = new Cursol("text");

        var result = ParseResult<string>.Failure(cursol);

        result.IsSuccess.Should().BeFalse();
        result.Cursol.Should().Be(cursol);
    }

    [Fact]
    public void 失敗結果から値は取得できません()
    {
        var result = ParseResult<string>.Failure(new Cursol("text"));

        var action = () => result.Parsed;

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void 成功結果にnullは指定できません()
    {
        var action = () => ParseResult<string>.Success(new Cursol("text"), null);

        action.Should().Throw<ArgumentNullException>();
    }
}
