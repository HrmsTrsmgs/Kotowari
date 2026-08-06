using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class ConditionalCharParserのテスト
{
    [Fact]
    public void ParseAsyncは指定した文字を読み込みに成功します()
    {
        var cursol = new Cursol("public");
        var tested = new ConditionalCharParser(c => c == 'p');

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncは指定した文字を読み込みます()
    {
        var cursol = new Cursol("public");
        var tested = new ConditionalCharParser(c => c == 'p');

        var result = tested.Parse(cursol);

        result.Parsed.Should().Be('p');
    }

    [Fact]
    public void ParseAsyncは指定していない文字を読み込みに失敗します()
    {
        var cursol = new Cursol("internal");
        var tested = new ConditionalCharParser(c => c == 'p');

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは読み込みに成功した場合にその分進んだカーソルを返します()
    {
        var cursol = new Cursol("public");
        var tested = new ConditionalCharParser(c => c == 'p');

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(1);
    }
}
