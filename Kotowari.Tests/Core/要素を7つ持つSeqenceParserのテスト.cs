using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を7つ持つSeqenceParserのテスト
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
    [Fact(Skip = "[空白責務分離 053] WithWhiteSpaceが未実装のため。親に指定した空白規則で各単語を読み込める実装後に解除します。")]
    public void ParseAsyncは指定した単語を読み込みに成功します()
    {
        var cursol = new Cursol("ab cd ef gh ij kl mn");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(Skip = "[空白責務分離 054] WithWhiteSpaceが未実装のため。親に空白規則を指定しても1番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncは一つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ax cd ef gh ij kl mn");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 055] WithWhiteSpaceが未実装のため。親に空白規則を指定しても2番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncはふたつ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cx ef gh ij kl mn");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }
    [Fact(Skip = "[空白責務分離 056] WithWhiteSpaceが未実装のため。親に空白規則を指定しても3番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncは三つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ex gh ij kl mn");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 057] WithWhiteSpaceが未実装のため。親に空白規則を指定しても4番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncは4つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gx ij kl mn");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 058] WithWhiteSpaceが未実装のため。親に空白規則を指定しても5番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncは5つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gh ix kl mn");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 059] WithWhiteSpaceが未実装のため。親に空白規則を指定しても6番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncは6つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gh ij kx mn");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 060] WithWhiteSpaceが未実装のため。親に空白規則を指定しても7番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncは7つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gh ij kl m");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 061] WithWhiteSpaceが未実装のため。親に空白規則を指定して各単語の値を返す実装後に解除します。")]
    public void ParseAsyncは指定した単語を読み込みます()
    {
        var cursol = new Cursol("ab cd ef gh ij kl mn");
        var result = Tested.Parse(cursol);

        result.Parsed.Should().Be(("ab", "cd", "ef", "gh", "ij", "kl", "mn"));
    }

    [Fact(Skip = "[空白責務分離 062] WithWhiteSpaceが未実装のため。親に指定した空白規則を使って解析後の位置を返す実装後に解除します。")]
    public void ParseAsyncは読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
    {
        var cursol = new Cursol("ab cd ef gh ij kl mn");

        var result = Tested.Parse(cursol);

        result.Cursol.Index.Should().Be("ab cd ef gh ij kl mn".Length);
    }
}
