using FluentAssertions;
using Marimo.Kotowari.Core;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class ConditionalCharParserのテスト
{
    [Fact]
    public void 指定した文字の読み込みに成功します()
    {
        var cursol = new Cursol("public");
        var tested = new ConditionalCharParser(c => c == 'p');

        tested.Parse(cursol).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void 指定した文字を読み込みます()
    {
        var cursol = new Cursol("public");
        var tested = new ConditionalCharParser(c => c == 'p');

        tested.Parse(cursol).Parsed.Should().Be('p');
    }

    [Fact]
    public void 指定していない文字の読み込みに失敗します()
    {
        var cursol = new Cursol("internal");
        var tested = new ConditionalCharParser(c => c == 'p');

        tested.Parse(cursol).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void 読み込みに成功した場合にその分進んだカーソルを返します()
    {
        var cursol = new Cursol("public");
        var tested = new ConditionalCharParser(c => c == 'p');

        tested.Parse(cursol).Cursol.Index.Should().Be(1);
    }
}
