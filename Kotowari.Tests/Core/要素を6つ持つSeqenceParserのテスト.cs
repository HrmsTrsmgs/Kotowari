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
    Parser<(string, string, string, string, string, string)> Tested { get; }

    public 要素を6つ持つSeqenceParserのテスト()
    {
        Tested = new SequenceParser<string, string, string, string, string, string>(
            new WordParser("ab"),
            new WordParser("cd"),
            new WordParser("ef"),
            new WordParser("gh"),
            new WordParser("ij"),
            new WordParser("kl"));
    }
    [Fact]
    public void ParseAsyncは指定した単語を読み込みに成功します()
    {
        var cursol = Cursol.FromString("ab cd ef gh ij kl");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncは一つ目の解析に失敗した場合は失敗します()
    {
        var cursol = Cursol.FromString("abc cd ef gh ij kl");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncはふたつ目の解析に失敗した場合は失敗します()
    {
        var cursol = Cursol.FromString("ab cde ef gh ij kl");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }
    [Fact]
    public void ParseAsyncは三つ目の解析に失敗した場合は失敗します()
    {
        var cursol = Cursol.FromString("ab cd efg gh ij kl");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは4つ目の解析に失敗した場合は失敗します()
    {
        var cursol = Cursol.FromString("ab cd ef ghi ij kl");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは5つ目の解析に失敗した場合は失敗します()
    {
        var cursol = Cursol.FromString("ab cd ef gh ijk kl");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは6つ目の解析に失敗した場合は失敗します()
    {
        var cursol = Cursol.FromString("ab cd ef gh ij k");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは指定した単語を読み込みます()
    {
        var cursol = Cursol.FromString("ab cd ef gh ij kl");
        var result = Tested.Parse(cursol);

        result.Parsed.Should().Be(("ab", "cd", "ef", "gh", "ij", "kl"));
    }

    [Fact]
    public void ParseAsyncは読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
    {
        var cursol = Cursol.FromString("ab cd ef gh ij kl");

        var result = Tested.Parse(cursol);

        result.Cursol.Index.Should().Be("ab cd ef gh ij kl".Length);
    }
}
