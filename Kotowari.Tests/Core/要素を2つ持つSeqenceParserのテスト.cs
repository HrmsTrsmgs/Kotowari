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

public class 要素を2つ持つSeqenceParserのテスト
{
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

    static Parser<(string, string)> Tested
        => SequenceParser.Create(new WordParser("public"), new WordParser("static"))
            .WithWhiteSpace(new CharParser(' '));

    [Fact(Skip = "[空白責務分離 018] WithWhiteSpaceが未実装のため。親に指定した空白規則で各単語を読み込める実装後に解除します。")]
    public void ParseAsyncは指定した単語を読み込みに成功します()
    {
        var cursol = new Cursol("public static");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(Skip = "[空白責務分離 019] WithWhiteSpaceが未実装のため。親に空白規則を指定しても一つ目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncは一つ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("publiac static");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 020] WithWhiteSpaceが未実装のため。親に空白規則を指定しても二つ目の不一致で失敗する実装後に解除します。")]
    public void ParseAsyncはふたつ目の解析に失敗した場合は失敗します()
    {
        var cursol = new Cursol("public statiac");

        var result = Tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "[空白責務分離 021] WithWhiteSpaceが未実装のため。親に空白規則を指定して各単語の値を返す実装後に解除します。")]
    public void ParseAsyncは指定した単語を読み込みます()
    {
        var cursol = new Cursol("public static");

        var result = Tested.Parse(cursol);

        result.Parsed.Should().Be(("public", "static"));
    }

    [Fact(Skip = "[空白責務分離 022] WithWhiteSpaceが未実装のため。親に指定した空白規則を使って解析後の位置を返す実装後に解除します。")]
    public void ParseAsyncは読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
    {
        var cursol = new Cursol("public static");
        var tested = SequenceParser.Create(new WordParser("public"), new WordParser("static"))
            .WithWhiteSpace(new CharParser(' '));

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be("public static".Length);
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
