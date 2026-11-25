using FluentAssertions;
using Marimo.Kotowari.Core;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class ParserConverterのテスト
{
    [Fact]
    public void 子のnull成功値を変換関数に渡します()
    {
        var tested = ParserConverter.Create<string, string?>(
            ParserConverter.Create<string?, char>(new CharParser('a'), _ => null),
            it => it is null ? "null" : it);

        var result = tested.Parse(new Cursol("a!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("null");
        result.Cursol.Index.Should().Be(1);
    }

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
        => Tested = ParserConverter.Create(new WordParser("123"), int.Parse);

    [Fact]
    public void パースします()
        => Tested.Parse(new Cursol("123"));

    [Fact]
    public void 指定したパーサーと同じ条件で成功します()
        => Tested.Parse(new Cursol("123")).IsSuccess.Should().BeTrue();

    [Fact]
    public void 指定したパーサーと同じ条件で失敗します()
        => Tested.Parse(new Cursol("124")).IsSuccess.Should().BeFalse();

    [Fact]
    public void 指定した通り変換がなされます()
        => Tested.Parse(new Cursol("123")).Parsed.Should().Be(123);

    [Fact]
    public void 成功した時はカーソルが進みます()
        => Tested.Parse(new Cursol("123")).Cursol.Index.Should().Be(3);

    [Fact]
    public void 失敗した時はカーソルが進みません()
        => Tested.Parse(new Cursol("124")).Cursol.Index.Should().Be(0);
}
