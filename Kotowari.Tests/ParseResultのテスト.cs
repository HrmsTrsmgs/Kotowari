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
        var cursol = Cursol.FromString("text");

        var result = ParseResult<string, StringDocument>.Success(cursol, "parsed");

        result.IsSuccess.Should().BeTrue();
        (result.Cursol == cursol).Should().BeTrue();
        result.Parsed.Should().Be("parsed");
    }

    [Fact]
    public void 失敗結果はカーソルを保持します()
    {
        var cursol = Cursol.FromString("text");

        var result = ParseResult<string, StringDocument>.Failure(cursol);

        result.IsSuccess.Should().BeFalse();
        (result.Cursol == cursol).Should().BeTrue();
    }

    [Fact]
    public void 失敗結果から値は取得できません()
    {
        var action = () => ParseResult<string, StringDocument>.Failure(Cursol.FromString("text")).Parsed;

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void 成功結果にnullは指定できません()
    {
        Action action = () => ParseResult<string, StringDocument>.Success(Cursol.FromString("text"), null!);

        action.Should().Throw<ArgumentNullException>();
    }
}
