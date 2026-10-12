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
        (result.Cursol == cursol).Should().BeTrue();
        result.Parsed.Should().Be("parsed");
    }

    [Fact]
    public void 失敗結果はカーソルを保持します()
    {
        var cursol = new Cursol("text");

        var result = ParseResult<string>.Failure(cursol);

        result.IsSuccess.Should().BeFalse();
        (result.Cursol == cursol).Should().BeTrue();
    }

    [Fact]
    public void 失敗結果から値は取得できません()
    {
        var action = () => ParseResult<string>.Failure(new Cursol("text")).Parsed;

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void 成功結果にnullは指定できません()
    {
        Action action = () => ParseResult<string>.Success(new Cursol("text"), null!);

        action.Should().Throw<ArgumentNullException>();
    }
}
