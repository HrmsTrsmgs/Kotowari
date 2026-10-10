using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class RecursiveParserのテスト
{
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

        tested.Parse(Cursol.FromString("!")).IsSuccess.Should().BeFalse();
        tested.Parse(Cursol.FromUtf8("!"u8)).IsSuccess.Should().BeFalse();
        呼び出し回数.Should().Be(0);
    }

    [Fact]
    public void 再帰getterが毎回作る変換パーサーもその呼び出しの定義を使います()
    {
        var 呼び出し回数 = 0;
        var tested = new ZeroOrMoreParser<char>(new RecursiveParser<char>(
            () => new ParserConverter<char, char>(
                new CharParser(++呼び出し回数 == 1 ? 'a' : 'あ'), char.ToUpper)));

        var utf16 = tested.Parse(Cursol.FromString("aあ!"));
        utf16.Parsed.Should().Equal('A', 'あ');
        utf16.Cursol.Index.Should().Be(2);
        呼び出し回数.Should().Be(3);

        呼び出し回数 = 0;
        var utf8 = tested.Parse(Cursol.FromUtf8("aあ!"u8));
        utf8.Parsed.Should().Equal('A', 'あ');
        utf8.Cursol.Index.Should().Be(4);
        呼び出し回数.Should().Be(3);
    }

    [Fact]
    public void ParseAsyncは内部パーサーと同じように成功します()
    {
        var cursol = Cursol.FromString("public");
        var parser = new CharParser('p');
        var tested = new RecursiveParser<char>(() => parser);
        var result = tested.Parse(cursol);
        var expected = parser.Parse(cursol);
        result.IsSuccess.Should().Be(expected.IsSuccess);
        (result.Cursol == expected.Cursol).Should().BeTrue();
        result.Parsed.Should().Be(expected.Parsed);
    }
    [Fact]
    public void ParseAsyncは内部パーサーと同じように失敗します()
    {
        var cursol = Cursol.FromString("public");
        var parser = new CharParser('a');
        var tested = new RecursiveParser<char>(() => parser);
        var result = tested.Parse(cursol);
        var expected = parser.Parse(cursol);
        result.IsSuccess.Should().Be(expected.IsSuccess);
        (result.Cursol == expected.Cursol).Should().BeTrue();
        var readParsed = () => tested.Parse(Cursol.FromString("public")).Parsed;
        var readExpected = () => parser.Parse(Cursol.FromString("public")).Parsed;
        readParsed.Should().Throw<InvalidOperationException>();
        readExpected.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void 組み合わせの各呼び出しでgetterを評価しその時のパーサーを使います()
    {
        var 呼び出し回数 = 0;
        var tested = new ZeroOrMoreParser<char>(new RecursiveParser<char>(
            () => new CharParser(++呼び出し回数 == 1 ? 'a' : 'あ')));

        var utf16 = tested.Parse(Cursol.FromString("aあ!"));

        utf16.Parsed.Should().Equal('a', 'あ');
        utf16.Cursol.Index.Should().Be(2);
        呼び出し回数.Should().Be(3);

        呼び出し回数 = 0;
        var utf8 = tested.Parse(Cursol.FromUtf8("aあ!"u8));

        utf8.Parsed.Should().Equal('a', 'あ');
        utf8.Cursol.Index.Should().Be(4);
        呼び出し回数.Should().Be(3);
    }
}
