using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を10持つSeqenceParserのテスト
{
    static Parser<(string, string, string, string, string, string, string, string, string, string)> Tested
        => new SequenceParser<string, string, string, string, string, string, string, string, string, string>(
            new WordParser("ab"),
            new WordParser("cd"),
            new WordParser("ef"),
            new WordParser("gh"),
            new WordParser("ij"),
            new WordParser("kl"),
            new WordParser("mn"),
            new WordParser("op"),
            new WordParser("qr"),
            new WordParser("st"))
            .WithWhiteSpace(new CharParser(' '));
    [Fact]
    public void 親に指定した空白規則で十個の単語を読み込み末尾まで進みます()
    {
        var result = Tested.Parse(new Cursol("ab cd ef gh ij kl mn op qr st"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("ab", "cd", "ef", "gh", "ij", "kl", "mn", "op", "qr", "st"));
        result.Cursol.Index.Should().Be("ab cd ef gh ij kl mn op qr st".Length);
    }

    [Theory]
    [InlineData("ax cd ef gh ij kl mn op qr st")]
    [InlineData("ab cx ef gh ij kl mn op qr st")]
    [InlineData("ab cd ex gh ij kl mn op qr st")]
    [InlineData("ab cd ef gx ij kl mn op qr st")]
    [InlineData("ab cd ef gh ix kl mn op qr st")]
    [InlineData("ab cd ef gh ij kx mn op qr st")]
    [InlineData("ab cd ef gh ij kl mx op qr st")]
    [InlineData("ab cd ef gh ij kl mn ox qr st")]
    [InlineData("ab cd ef gh ij kl mn op qx st")]
    [InlineData("ab cd ef gh ij kl mn op qr s")]
    public void 親に空白規則を指定してもいずれかの単語が一致しなければ失敗します(string 入力)
    {
        Tested.Parse(new Cursol(入力)).IsSuccess.Should().BeFalse();
    }
}
