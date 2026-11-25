using FluentAssertions;
using Marimo.Kotowari.Core;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class ExpectCharParserのテスト
{
    [Fact]
    public void 組み合わせから呼んでも派生否定パーサーの処理と基底実装を使います()
    {
        var parser = new UpperCaseExpectCharParser();
        var tested = new ZeroOrMoreParser<char>(parser);

        var result = tested.Parse(new Cursol("ab!"));

        result.Parsed.Should().Equal('A', 'B');
        result.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(3);
    }

    [Fact]
    public void 否定対象が消費して失敗しても自身の開始位置から一文字だけ読みます()
    {
        var tested = new ExpectCharParser(new ConsumingFailureCharParser());

        var result = tested.Parse(new Cursol("あb"));

        result.Parsed.Should().Be('あ');
        result.Cursol.Index.Should().Be(1);
    }

    [Fact]
    public void 指定していない文字の読み込みに成功します()
    {
        var cursol = new Cursol("public");
        var tested = new ExpectCharParser(
            new CharParser('a'));

        tested.Parse(cursol).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void 指定していない文字を読み込みます()
    {
        var cursol = new Cursol("public");
        var tested = new ExpectCharParser(
            new CharParser('a'));

        tested.Parse(cursol).Parsed.Should().Be('p');
    }

    [Fact]
    public void 指定した文字の読み込みに失敗します()
    {
        var cursol = new Cursol("public");
        var tested = new ExpectCharParser(
            new CharParser('p'));

        tested.Parse(cursol).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void 読み込みに成功した場合にその分進んだカーソルを返します()
    {
        var tested = new ExpectCharParser(
            new CharParser('a'));

        var cursol = tested.Parse(new Cursol("public")).Cursol;

        cursol.Index.Should().Be(1);
    }

    [Fact]
    public void 読み込みに成功した場合に読み込んだ文字を返します()
    {
        var tested = new ExpectCharParser(
            new CharParser('a'));

        tested.Parse(new Cursol("public")).Parsed.Should().Be('p');
    }
}
