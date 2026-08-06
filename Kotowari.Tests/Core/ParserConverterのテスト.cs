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
        var isSuccess = Tested.Parse(new Cursol("123")).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void 指定したパーサーと同じ条件で失敗します()
    {
        var isSuccess = Tested.Parse(new Cursol("124")).IsSuccess;

        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void 指定した通り変換がなされます()
    {
        var parsed = Tested.Parse(new Cursol("123")).Parsed;

        parsed.Should().Be(123);
    }

    [Fact]
    public void 成功した時はカーソルが進みます()
    {
        var cursol = Tested.Parse(new Cursol("123")).Cursol;

        cursol.Index.Should().Be(3);
    }
    [Fact]
    public void 失敗した時はカーソルが進みません()
    {
        var cursol = Tested.Parse(new Cursol("124")).Cursol;

        cursol.Index.Should().Be(0);
    }
}
