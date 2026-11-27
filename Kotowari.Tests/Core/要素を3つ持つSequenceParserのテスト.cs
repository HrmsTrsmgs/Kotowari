using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を3つ持つSequenceParserのテスト
{
    [Fact]
    public void 間の子がnullで成功しても前後の値とともに保持します()
    {
        var tested = SequenceParser.Create(
            new CharParser('a'),
            ParserConverter.Create<string?, char>(new CharParser('b'), _ => null),
            new CharParser('c'));

        var result = tested.Parse(new Cursol("abc!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(('a', (string?)null, 'c'));
        result.Cursol.Index.Should().Be(3);
    }

    static Parser<(string, string, string)> Tested
        => SequenceParser.Create(
            new WordParser("public"),
            new WordParser("static"),
            new WordParser("int"))
            .WithWhiteSpace(new CharParser(' '));

    [Fact]
    public void 親に指定した空白規則で三つの単語を読み込み末尾まで進みます()
    {
        var result = Tested.Parse(new Cursol("public static int"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("public", "static", "int"));
        result.Cursol.Index.Should().Be("public static int".Length);
    }

    [Theory]
    [InlineData("publiac static int")]
    [InlineData("public statiac int")]
    [InlineData("public static innt")]
    public void 親に空白規則を指定してもいずれかの単語が一致しなければ失敗します(string 入力)
    {
        Tested.Parse(new Cursol(入力)).IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void 子が消費して失敗しても開始位置に戻り後続は呼びません(int 失敗箇所)
    {
        var 呼び出し = new List<(int 順番, int 位置)>();
        CallbackCharParser 子パーサー(int 順番)
            => new((current, index) =>
            {
                呼び出し.Add((順番, index));
                return (順番 != 失敗箇所, current, 1);
            });
        var tested = new SequenceParser<char, char, char>(
            子パーサー(1), 子パーサー(2), 子パーサー(3));
        var start = new Cursol("_abc!").GoFoward(1);

        var result = tested.Parse(start);

        result.IsSuccess.Should().BeFalse();
        result.Cursol.Should().Be(start);
        呼び出し.Should().Equal(from it in Enumerable.Range(1, 失敗箇所) select (it, it));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void 子の例外をそのまま伝播し後続は呼びません(int 例外箇所)
    {
        var exception = new InvalidOperationException();
        var 呼び出し = new List<int>();
        CallbackCharParser 子パーサー(int 順番)
            => new((current, _) =>
            {
                呼び出し.Add(順番);
                if (順番 == 例外箇所) throw exception;
                return (true, current, 1);
            });
        var tested = new SequenceParser<char, char, char>(
            子パーサー(1), 子パーサー(2), 子パーサー(3));

        var parse = () => tested.Parse(new Cursol("abc!"));

        parse.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
        呼び出し.Should().Equal(Enumerable.Range(1, 例外箇所));
    }

    [Fact]
    public void 子の派生シーケンスと文字パーサーの成功値をそれぞれ使います()
    {
        var first = new UpperCaseSequenceParser();
        var second = new UpperCaseCharParser();
        var third = new UpperCaseCharParser();
        var tested = new SequenceParser<(char, char), char, char>(first, second, third);

        var result = tested.Parse(new Cursol("_abaa!").GoFoward(1));

        result.Parsed.Should().Be((('A', 'B'), 'A', 'A'));
        result.Cursol.Index.Should().Be(5);
        first.呼び出し回数.Should().Be(1);
        second.呼び出し回数.Should().Be(1);
        third.呼び出し回数.Should().Be(1);
    }
}
