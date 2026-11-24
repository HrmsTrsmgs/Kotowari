using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を6つ持つSeqenceParserのテスト
{
    static Parser<(string, string, string, string, string, string)> Tested
        => new SequenceParser<string, string, string, string, string, string>(
            new WordParser("ab"),
            new WordParser("cd"),
            new WordParser("ef"),
            new WordParser("gh"),
            new WordParser("ij"),
            new WordParser("kl"))
            .WithWhiteSpace(new CharParser(' '));
    [Fact]
    public void 親に指定した空白規則で六つの単語を読み込み末尾まで進みます()
    {
        var result = Tested.Parse(new Cursol("ab cd ef gh ij kl"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("ab", "cd", "ef", "gh", "ij", "kl"));
        result.Cursol.Index.Should().Be("ab cd ef gh ij kl".Length);
    }

    [Theory]
    [InlineData("ax cd ef gh ij kl")]
    [InlineData("ab cx ef gh ij kl")]
    [InlineData("ab cd ex gh ij kl")]
    [InlineData("ab cd ef gx ij kl")]
    [InlineData("ab cd ef gh ix kl")]
    [InlineData("ab cd ef gh ij k")]
    public void 親に空白規則を指定してもいずれかの単語が一致しなければ失敗します(string 入力)
    {
        Tested.Parse(new Cursol(入力)).IsSuccess.Should().BeFalse();
    }
}
