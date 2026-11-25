using FluentAssertions;
using Marimo.Kotowari.Core;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を5つ持つSequenceParserのテスト
{
    static Parser<(string, string, string, string, string)> Tested
        => new SequenceParser<string, string, string, string, string>(
            new WordParser("ab"),
            new WordParser("cd"),
            new WordParser("ef"),
            new WordParser("gh"),
            new WordParser("ij"))
            .WithWhiteSpace(new CharParser(' '));
    [Fact]
    public void 親に指定した空白規則で五つの単語を読み込み末尾まで進みます()
    {
        var result = Tested.Parse(new Cursol("ab cd ef gh ij"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("ab", "cd", "ef", "gh", "ij"));
        result.Cursol.Index.Should().Be("ab cd ef gh ij".Length);
    }

    [Theory]
    [InlineData("ax cd ef gh ij")]
    [InlineData("ab cx ef gh ij")]
    [InlineData("ab cd ex gh ij")]
    [InlineData("ab cd ef gx ij")]
    [InlineData("ab cd ef gh i")]
    public void 親に空白規則を指定してもいずれかの単語が一致しなければ失敗します(string 入力)
    {
        Tested.Parse(new Cursol(入力)).IsSuccess.Should().BeFalse();
    }
}
