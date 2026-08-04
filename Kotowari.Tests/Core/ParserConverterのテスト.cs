using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class ParserConverterのテスト
{
    ParserConverter<string, int> Tested { get; }

    public ParserConverterのテスト()
        => Tested = ParserConverter.Create(new WordParser("123"), s => int.Parse(s));


    [Fact]
    public void パースします()
        => Tested.Parse(new Cursol("123"));

    [Fact]
    public void 指定したパーサーと同じ条件で成功します()
    {
        var (isSuccess, _, _) = Tested.Parse(new Cursol("123"));

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void 指定したパーサーと同じ条件で失敗します()
    {
        var (isSuccess, _, _) = Tested.Parse(new Cursol("124"));

        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void 指定した通り変換がなされます()
    {
        var (_, _, parsed) = Tested.Parse(new Cursol("123"));

        parsed.Should().Be(123);
    }

    [Fact]
    public void 成功した時はカーソルが進みます()
    {
        var (_, cursol, _) = Tested.Parse(new Cursol("123"));

        cursol.Index.Should().Be(3);
    }
    [Fact]
    public void 失敗した時はカーソルが進みません()
    {
        var (_, cursol, _) = Tested.Parse(new Cursol("124"));

        cursol.Index.Should().Be(0);
    }
}
