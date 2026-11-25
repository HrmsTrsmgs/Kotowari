using FluentAssertions;
using Marimo.Kotowari.Core;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class CharParserのテスト
{

    [Fact]
    public void 指定した文字の読み込みに成功します()
    {
        var cursol = new Cursol("public");
        var tested = new CharParser('p');

        tested.Parse(cursol).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void 指定した文字を読み込みます()
    {
        var cursol = new Cursol("public");
        var tested = new CharParser('p');

        tested.Parse(cursol).Parsed.Should().Be('p');
    }

    [Fact]
    public void 指定していない文字の読み込みに失敗します()
    {
        var cursol = new Cursol("internal");
        var tested = new CharParser('p');

        tested.Parse(cursol).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void 読み込みに成功した場合にその分進んだカーソルを返します()
    {
        var cursol = new Cursol("public");
        var tested = new CharParser('p');

        tested.Parse(cursol).Cursol.Index.Should().Be(1);
    }

    [Fact]
    public void IgnoreCaseでない場合に小文字指定の場合に大文字を区別します()
    {
        var cursol = new Cursol("Public");
        var tested = new CharParser('p');

        tested.Parse(cursol).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void IgnoreCaseでない場合に大文字指定の場合に小文字を区別します()
    {
        var cursol = new Cursol("public");
        var tested = new CharParser('P');

        tested.Parse(cursol).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void IgnoreCaseで小文字指定でも大文字を識別します()
    {
        var cursol = new Cursol("Public");
        var tested = new CharParser('p', true);

        tested.Parse(cursol).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IgnoreCaseで大文字指定でも小文字を識別します()
    {
        var cursol = new Cursol("public");
        var tested = new CharParser('P', true);

        tested.Parse(cursol).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IgnoreCaseで小文字指定の場合に実際に識別した文字を結果とします()
    {
        var cursol = new Cursol("Public");
        var tested = new CharParser('p', true);

        tested.Parse(cursol).Parsed.Should().Be('P');
    }

    [Fact]
    public void IgnoreCaseで大文字指定の場合に実際に識別した文字を結果とします()
    {
        var cursol = new Cursol("public");
        var tested = new CharParser('P', true);

        tested.Parse(cursol).Parsed.Should().Be('p');
    }

    [Theory]
    [InlineData("ab", 'a')]
    [InlineData("éb", 'é')]
    [InlineData("あb", 'あ')]
    public void 文字列入力を同じ文字パーサーで読み込めます(string input, char first)
    {
        Parser<char> tested = new CharParser(first);

        var result = tested.Parse(new Cursol(input));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(first);
        result.Cursol.Index.Should().Be(1);
    }

    [Fact]
    public void 組み合わせから呼んでも派生文字パーサーの処理と基底実装を使います()
    {
        var parser = new UpperCaseCharParser();
        var tested = new ZeroOrMoreParser<char>(
            new OrParser<char>(parser, new CharParser('b')));

        var result = tested.Parse(new Cursol("ab!"));

        result.Parsed.Should().Equal('A', 'b');
        result.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(3);
    }
}
