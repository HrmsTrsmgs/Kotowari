using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class RecursiveParserのテスト
{
    [Fact]
    public void 再帰先のnull成功値を失敗と区別して返します()
    {
        var tested = new RecursiveParser<string?>(() =>
            ParserConverter.Create<string?, char>(new CharParser('a'), _ => null));

        var success = tested.Parse(new Cursol("a!"));
        var failure = tested.Parse(new Cursol("!"));

        success.IsSuccess.Should().BeTrue();
        success.Parsed.Should().BeNull();
        success.Cursol.Index.Should().Be(1);
        failure.IsSuccess.Should().BeFalse();
        var readFailure = () => failure.Parsed;
        readFailure.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void シーケンスが先に失敗した場合は再帰getterを評価しません()
    {
        var 呼び出し回数 = 0;
        var tested = new SequenceParser<char, char>(
            new CharParser('a'),
            new RecursiveParser<char>(() =>
            {
                呼び出し回数++;
                return new CharParser('b');
            }));

        tested.Parse(new Cursol("!")).IsSuccess.Should().BeFalse();
        呼び出し回数.Should().Be(0);
    }

    [Fact]
    public void 再帰getterが毎回作る変換パーサーもその呼び出しの定義を使います()
    {
        var 呼び出し回数 = 0;
        var tested = new ZeroOrMoreParser<char>(new RecursiveParser<char>(
            () => new ParserConverter<char, char>(
                new CharParser(++呼び出し回数 == 1 ? 'a' : 'あ'), char.ToUpper)));

        var result = tested.Parse(new Cursol("aあ!"));
        result.Parsed.Should().Equal('A', 'あ');
        result.Cursol.Index.Should().Be(2);
        呼び出し回数.Should().Be(3);
    }

    [Fact]
    public void 内部パーサーと同じように成功します()
    {
        var cursol = new Cursol("public");
        var parser = new CharParser('p');
        var tested = new RecursiveParser<char>(() => parser);
        var result = tested.Parse(cursol);
        var expected = parser.Parse(cursol);
        result.IsSuccess.Should().Be(expected.IsSuccess);
        (result.Cursol == expected.Cursol).Should().BeTrue();
        result.Parsed.Should().Be(expected.Parsed);
    }
    [Fact]
    public void 内部パーサーと同じように失敗します()
    {
        var cursol = new Cursol("public");
        var parser = new CharParser('a');
        var tested = new RecursiveParser<char>(() => parser);
        var result = tested.Parse(cursol);
        var expected = parser.Parse(cursol);
        result.IsSuccess.Should().Be(expected.IsSuccess);
        (result.Cursol == expected.Cursol).Should().BeTrue();
        var readParsed = () => tested.Parse(new Cursol("public")).Parsed;
        var readExpected = () => parser.Parse(new Cursol("public")).Parsed;
        readParsed.Should().Throw<InvalidOperationException>();
        readExpected.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void 組み合わせの各呼び出しでgetterを評価しその時のパーサーを使います()
    {
        var 呼び出し回数 = 0;
        var tested = new ZeroOrMoreParser<char>(new RecursiveParser<char>(
            () => new CharParser(++呼び出し回数 == 1 ? 'a' : 'あ')));

        var result = tested.Parse(new Cursol("aあ!"));

        result.Parsed.Should().Equal('a', 'あ');
        result.Cursol.Index.Should().Be(2);
        呼び出し回数.Should().Be(3);
    }
}
