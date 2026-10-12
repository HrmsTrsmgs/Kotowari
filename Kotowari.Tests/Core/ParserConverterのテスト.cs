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
    [Fact]
    public void 組み合わせから呼んでも派生変換パーサーの処理と基底実装を使います()
    {
        var parser = new IncrementingParserConverter();
        var tested = new ZeroOrMoreParser<int>(parser);

        var result = tested.Parse(new Cursol("aa!"));

        result.Parsed.Should().Equal(98, 98);
        result.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(3);
    }

    [Fact]
    public void 再帰getterが返す派生変換パーサーも上書きされた処理を使います()
    {
        var parser = new IncrementingParserConverter();
        var getter呼び出し回数 = 0;
        var tested = new ZeroOrMoreParser<int>(new RecursiveParser<int>(() =>
        {
            getter呼び出し回数++;
            return parser;
        }));

        var result = tested.Parse(new Cursol("aa!"));

        result.Parsed.Should().Equal(98, 98);
        result.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(3);
        getter呼び出し回数.Should().Be(3);
    }

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
