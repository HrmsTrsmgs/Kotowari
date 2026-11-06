using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を3つ持つSeqenceParserのテスト
{
    static Parser<(string, string, string)> Tested
        => SequenceParser.Create(
            new WordParser("public"),
            new WordParser("static"),
            new WordParser("int"))
            .WithWhiteSpace(new CharParser(' '));

    [Fact(Skip = "[空白責務分離 023] WithWhiteSpaceが未実装のため。親に指定した空白規則で各単語を読み込める実装後に解除します。")]
    public void ParseAsyncは指定した単語を読み込みに成功します()
    {
        var cursol = new Cursol("public static int");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(Skip = "[空白責務分離 024] WithWhiteSpaceが未実装のため。親に空白規則を指定しても1番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncは一つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("publiac static int");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 025] WithWhiteSpaceが未実装のため。親に空白規則を指定しても2番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncはふたつ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("public statiac int");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 026] WithWhiteSpaceが未実装のため。親に空白規則を指定しても3番目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncは三つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("public static innt");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 027] WithWhiteSpaceが未実装のため。親に空白規則を指定して各単語の値を返す実装後に解除します。")]
    public void ParseAsyncは指定した単語を読み込みます()
    {
        var cursol = new Cursol("public static int");

        var result = Tested.Parse(cursol);

        result.Parsed.Should().Be(("public", "static", "int"));
    }

    [Fact(Skip = "[空白責務分離 028] WithWhiteSpaceが未実装のため。親に指定した空白規則を使って解析後の位置を返す実装後に解除します。")]
    public void ParseAsyncは読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
    {
        var cursol = new Cursol("public static int");

        var result = Tested.Parse(cursol);

        result.Cursol.Index.Should().Be("public static int".Length);
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
