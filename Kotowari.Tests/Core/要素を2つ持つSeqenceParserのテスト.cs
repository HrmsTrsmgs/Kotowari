using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を2つ持つSeqenceParserのテスト
{
    [Fact]
    public void 組み合わせから呼んでも派生シーケンスの処理と基底実装を使います()
    {
        var parser = new UpperCaseSequenceParser();
        var tested = new OrParser<(char, char)>(parser);

        var utf16 = tested.Parse(Cursol.FromString("ab!"));
        var utf8 = tested.Parse(Cursol.FromUtf8("ab!"u8));

        utf16.Parsed.Should().Be(('A', 'B'));
        utf16.Cursol.Index.Should().Be(2);
        utf8.Parsed.Should().Be(('A', 'B'));
        utf8.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(2);
    }

    Parser<(string, string)> Tested { get; }

    public 要素を2つ持つSeqenceParserのテスト()
    {
        Tested = new SequenceParser<string, string>(
            new WordParser("public"),
            new WordParser("static"));
    }

    [Fact]
    public void ParseAsyncは指定した単語を読み込みに成功します()
    {
        var cursol = Cursol.FromString("public static");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncは一つ目の解析に失敗した場合は失敗します()
    {
        var cursol = Cursol.FromString("publica static");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncはふたつ目の解析に失敗した場合は失敗します()
    {
        var cursol = Cursol.FromString("public statiac");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは指定した単語を読み込みます()
    {
        var cursol = Cursol.FromString("public static");

        var result = Tested.Parse(cursol);

        result.Parsed.Should().Be(("public", "static"));
    }

    [Fact]
    public void ParseAsyncは読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
    {
        var cursol = Cursol.FromString("public static");
        var tested = SequenceParser.Create(new WordParser("public"), new WordParser("static"));

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be("public static".Length);
    }
}
