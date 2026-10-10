using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class ExpectCharParserのテスト
{
    [Fact]
    public void 組み合わせから呼んでも派生否定パーサーの処理と基底実装を使います()
    {
        var parser = new UpperCaseExpectCharParser();
        var tested = new ZeroOrMoreParser<char>(parser);

        var utf16 = tested.Parse(Cursol.FromString("ab!"));
        var utf8 = tested.Parse(Cursol.FromUtf8("ab!"u8));

        utf16.Parsed.Should().Equal('A', 'B');
        utf16.Cursol.Index.Should().Be(2);
        utf8.Parsed.Should().Equal('A', 'B');
        utf8.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(6);
    }

    [Fact]
    public void 否定対象が消費して失敗しても自身の開始位置から一文字だけ読みます()
    {
        var tested = new ExpectCharParser(new ConsumingFailureCharParser());

        var utf16 = tested.Parse(Cursol.FromString("あb"));
        var utf8 = tested.Parse(Cursol.FromUtf8("あb"u8));

        utf16.Parsed.Should().Be('あ');
        utf16.Cursol.Index.Should().Be(1);
        utf8.Parsed.Should().Be('あ');
        utf8.Cursol.Index.Should().Be(3);
    }

    [Fact]
    public void ParseAsyncは指定ていない文字を読み込みに成功します()
    {
        var cursol = Cursol.FromString("public");
        var tested = new ExpectCharParser(
            new CharParser('a'));

        var isSuccess = tested.Parse(cursol).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncは指定していない文字を読み込みます()
    {
        var cursol = Cursol.FromString("public");
        var tested = new ExpectCharParser(
            new CharParser('a'));

        var parsed = tested.Parse(cursol).Parsed;

        parsed.Should().Be('p');
    }

    [Fact]
    public void ParseAsyncは指定した文字を読み込みに失敗します()
    {
        var cursol = Cursol.FromString("public");
        var tested = new ExpectCharParser(
            new CharParser('p'));

        var isSuccess = tested.Parse(cursol).IsSuccess;

        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは読み込みに成功した場合にその分進んだカーソルを返します()
    {
        var tested = new ExpectCharParser(
            new CharParser('a'));

        var cursol = tested.Parse(Cursol.FromString("public")).Cursol;

        cursol.Index.Should().Be(1);
    }

    [Fact]
    public void ParseAsyncは読み込みに成功した場合に読み込んだ文字を返します()
    {
        var tested = new ExpectCharParser(
            new CharParser('a'));

        var parsed = tested.Parse(Cursol.FromString("public")).Parsed;

        parsed.Should().Be('p');
    }
}
