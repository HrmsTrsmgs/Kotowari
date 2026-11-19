using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を8つ持つSeqenceParserのテスト
{
    static Parser<(string, string, string, string, string, string, string, string)> Tested
        => new SequenceParser<string, string, string, string, string, string, string, string>(
            new WordParser("ab"),
            new WordParser("cd"),
            new WordParser("ef"),
            new WordParser("gh"),
            new WordParser("ij"),
            new WordParser("kl"),
            new WordParser("mn"),
            new WordParser("op"))
            .WithWhiteSpace(new CharParser(' '));
    [Fact]
    public void 親に指定した空白規則で八つの単語を読み込み末尾まで進みます()
    {
        var result = Tested.Parse(new Cursol("ab cd ef gh ij kl mn op"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("ab", "cd", "ef", "gh", "ij", "kl", "mn", "op"));
        result.Cursol.Index.Should().Be("ab cd ef gh ij kl mn op".Length);
    }

    [Theory]
    [InlineData("ax cd ef gh ij kl mn op")]
    [InlineData("ab cx ef gh ij kl mn op")]
    [InlineData("ab cd ex gh ij kl mn op")]
    [InlineData("ab cd ef gx ij kl mn op")]
    [InlineData("ab cd ef gh ix kl mn op")]
    [InlineData("ab cd ef gh ij kx mn op")]
    [InlineData("ab cd ef gh ij kl mx op")]
    [InlineData("ab cd ef gh ij kl mn o")]
    public void 親に空白規則を指定してもいずれかの単語が一致しなければ失敗します(string 入力)
    {
        Tested.Parse(new Cursol(入力)).IsSuccess.Should().BeFalse();
    }
}
