using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を2つ持つSequenceParserのテスト
{
    [Fact]
    public void 二つの子のnull成功値を保持します()
    {
        var tested = SequenceParser.Create(
            ParserConverter.Create<string?, char>(new CharParser('a'), _ => null),
            ParserConverter.Create<int?, char>(new CharParser('b'), _ => null));

        var result = tested.Parse(new Cursol("ab!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(((string?)null, (int?)null));
        result.Cursol.Index.Should().Be(2);
    }

    [Fact]
    public void 組み合わせから呼んでも派生シーケンスの処理と基底実装を使います()
    {
        var parser = new UpperCaseSequenceParser();
        var tested = new OrParser<(char, char)>(parser);

        var result = tested.Parse(new Cursol("ab!"));

        result.Parsed.Should().Be(('A', 'B'));
        result.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(1);
    }

    [Fact]
    public void 空白規則を指定しても派生シーケンスの解析を使います()
    {
        var tested = new UpperCaseSequenceParser().WithWhiteSpace(new CharParser(' '));

        tested.Parse(new Cursol("ab!")).Parsed.Should().Be(('A', 'B'));
    }

    static Parser<(string, string)> Tested
        => SequenceParser.Create(new WordParser("public"), new WordParser("static"))
            .WithWhiteSpace(new CharParser(' '));

    [Fact]
    public void 親に指定した空白規則で二つの単語を読み込み末尾まで進みます()
    {
        var result = Tested.Parse(new Cursol("public static"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("public", "static"));
        result.Cursol.Index.Should().Be("public static".Length);
    }

    [Theory]
    [InlineData("publiac static")]
    [InlineData("public statiac")]
    public void 親に空白規則を指定してもいずれかの単語が一致しなければ失敗します(string 入力)
    {
        Tested.Parse(new Cursol(入力)).IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void 子が消費して失敗しても開始位置に戻り後続は呼びません(int 失敗箇所)
    {
        var 呼び出し = new List<(int 順番, int 位置)>();
        CallbackCharParser 子パーサー(int 順番)
            => new((current, index) =>
            {
                呼び出し.Add((順番, index));
                return (順番 != 失敗箇所, current, 1);
            });
        var tested = new SequenceParser<char, char>(子パーサー(1), 子パーサー(2));
        var start = new Cursol("_ab!").GoFoward(1);

        var result = tested.Parse(start);

        result.IsSuccess.Should().BeFalse();
        result.Cursol.Should().Be(start);
        呼び出し.Should().Equal(from it in Enumerable.Range(1, 失敗箇所) select (it, it));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
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
        var tested = new SequenceParser<char, char>(子パーサー(1), 子パーサー(2));

        var parse = () => tested.Parse(new Cursol("ab!"));

        parse.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
        呼び出し.Should().Equal(Enumerable.Range(1, 例外箇所));
    }

    [Fact]
    public void 各子の派生処理を一度ずつ呼びその成功値を返します()
    {
        var first = new UpperCaseCharParser();
        var second = new UpperCaseCharParser();
        var tested = new SequenceParser<char, char>(first, second);

        var result = tested.Parse(new Cursol("_aa!").GoFoward(1));

        result.Parsed.Should().Be(('A', 'A'));
        result.Cursol.Index.Should().Be(3);
        first.呼び出し回数.Should().Be(1);
        second.呼び出し回数.Should().Be(1);
    }
}
