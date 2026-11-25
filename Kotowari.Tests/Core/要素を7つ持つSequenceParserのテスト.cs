using FluentAssertions;
using Marimo.Kotowari.Core;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を7つ持つSequenceParserのテスト
{
    static Parser<(string, string, string, string, string, string, string)> Tested
        => new SequenceParser<string, string, string, string, string, string, string>(
            new WordParser("ab"),
            new WordParser("cd"),
            new WordParser("ef"),
            new WordParser("gh"),
            new WordParser("ij"),
            new WordParser("kl"),
            new WordParser("mn"))
            .WithWhiteSpace(new CharParser(' '));
    [Fact]
    public void 親に指定した空白規則で七つの単語を読み込み末尾まで進みます()
    {
        var result = Tested.Parse(new Cursol("ab cd ef gh ij kl mn"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("ab", "cd", "ef", "gh", "ij", "kl", "mn"));
        result.Cursol.Index.Should().Be("ab cd ef gh ij kl mn".Length);
    }

    [Theory]
    [InlineData("ax cd ef gh ij kl mn")]
    [InlineData("ab cx ef gh ij kl mn")]
    [InlineData("ab cd ex gh ij kl mn")]
    [InlineData("ab cd ef gx ij kl mn")]
    [InlineData("ab cd ef gh ix kl mn")]
    [InlineData("ab cd ef gh ij kx mn")]
    [InlineData("ab cd ef gh ij kl m")]
    public void 親に空白規則を指定してもいずれかの単語が一致しなければ失敗します(string 入力)
    {
        Tested.Parse(new Cursol(入力)).IsSuccess.Should().BeFalse();
    }
}
