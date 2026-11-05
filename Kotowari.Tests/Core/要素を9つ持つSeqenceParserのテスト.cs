using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を9つ持つSeqenceParserのテスト
{
    Parser<(string, string, string, string, string, string, string, string, string)> Tested { get; }

    public 要素を9つ持つSeqenceParserのテスト()
    {
        Tested = new SequenceParser<string, string, string, string, string, string, string, string, string>(
            new WordParser("ab"),
            new WordParser("cd"),
            new WordParser("ef"),
            new WordParser("gh"),
            new WordParser("ij"),
            new WordParser("kl"),
            new WordParser("mn"),
            new WordParser("op"),
            new WordParser("qr"));
    }
    [Fact]
    public void ParseAsyncは指定した単語を読み込みに成功します()
    {
        var cursol = new Cursol("ab cd ef gh ij kl mn op qr");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncは一つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ax cd ef gh ij kl mn op qr");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncはふたつ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cx ef gh ij kl mn op qr");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }
    [Fact]
    public void ParseAsyncは三つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ex gh ij kl mn op qr");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは4つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gx ij kl mn op qr");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは5つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gh ix kl mn op qr");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは6つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gh ij kx mn op qr");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは7つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gh ij kl mx op qr");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは8つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gh ij kl mn ox qr");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは9つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("ab cd ef gh ij kl mn op q");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは指定した単語を読み込みます()
    {
        var cursol = new Cursol("ab cd ef gh ij kl mn op qr");
        var result = Tested.Parse(cursol);

        result.Parsed.Should().Be(("ab", "cd", "ef", "gh", "ij", "kl", "mn", "op", "qr"));
    }

    [Fact]
    public void ParseAsyncは読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
    {
        var cursol = new Cursol("ab cd ef gh ij kl mn op qr");

        var result = Tested.Parse(cursol);

        result.Cursol.Index.Should().Be("ab cd ef gh ij kl mn op qr".Length);
    }
}
