using FluentAssertions;
using Marimo.Kotowari.Core;
using System.Collections.Generic;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を10持つSequenceParserのテスト
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

    [Fact]
    public void 末尾の入れ子を開かず各境界で空白を読みます()
    {
        var positions = new List<int>();
        var whitespace = new CallbackCharParser((current, index) =>
        {
            positions.Add(index);
            return (current == '_', current, 1);
        });
        var last = SequenceParser.Create(new CharParser('j'), new CharParser('k'));
        var tested = new SequenceParser<char, char, char, char, char, char, char, char, char, (char, char)>(
            new CharParser('a'), new CharParser('b'), new CharParser('c'),
            new CharParser('d'), new CharParser('e'), new CharParser('f'),
            new CharParser('g'), new CharParser('h'), new CharParser('i'), last)
            .WithWhiteSpace(whitespace);

        var result = tested.Parse(new Cursol("a_b_c_d_e_f_g_h_i_jk_!"));

        result.IsSuccess.Should().BeTrue();
        result.Cursol.Index.Should().Be(21);
        positions.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20, 21);
        tested.Parse(new Cursol("a_b_c_d_e_f_g_h_i_j_k")).IsSuccess.Should().BeFalse();
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
