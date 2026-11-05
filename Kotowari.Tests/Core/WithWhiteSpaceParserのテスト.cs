using FluentAssertions;
using Marimo.Kotowari.Core;

using System;
using System.Collections.Generic;

using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class WithWhiteSpaceParserのテスト
{
    [Fact]
    public void 空白が入力を消費せず成功したら例外を投げます()
    {
        var attempts = 0;
        var tested = new WithWhiteSpaceParser<char>(
            new CallbackCharParser((_, _) =>
                ++attempts == 1
                    ? (true, ' ', 0)
                    : throw new NotSupportedException("反復が停止しませんでした")),
            new CharParser('a'));
        var parse = () => tested.Parse(new Cursol("a"));

        parse.Should().Throw<InvalidOperationException>();
    }

    [Fact(Skip = "WithWhiteSpaceParserが前置空白だけを読んでいるため。子の成功後に空白を読み最後の成功位置を返す実装後に解除します。")]
    public void 子の後の空白パーサーが消費して失敗しても最後に成功した位置を返します()
    {
        var 呼び出し位置 = new List<int>();
        var tested = new WithWhiteSpaceParser<char>(new CallbackCharParser((current, index) =>
        {
            呼び出し位置.Add(index);
            return (current == '_', current, 1);
        }), new CharParser('あ'));

        var result = tested.Parse(new Cursol("あ__!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be('あ');
        result.Cursol.Index.Should().Be(3);
        result.Cursol.Current.Should().Be('!');
        呼び出し位置.Should().Equal(1, 2, 3);
    }

    [Fact(Skip = "WithWhiteSpaceParserが前置空白を読んでいるため。子を先に解析する実装後に解除します。")]
    public void 子の前にある空白は読み飛ばしません()
    {
        var tested = new WithWhiteSpaceParser<char>(new CharParser('_'), new CharParser('あ'));

        tested.Parse(new Cursol("_あ")).IsSuccess.Should().BeFalse();
    }

    [Fact(Skip = "WithWhiteSpaceParserが後置空白を読まないため。子の値を保ち指定した後置空白だけを読む実装後に解除します。")]
    public void 子の値を返して文法で指定した後置空白だけを読み飛ばします()
    {
        var tested = new WithWhiteSpaceParser<string>(new CharParser('_'), new WordParser("public"));

        var result = tested.Parse(new Cursol("public__ !"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("public");
        result.Cursol.Index.Should().Be(8);
        result.Cursol.Current.Should().Be(' ');
    }

    [Fact]
    public void 前の空白を読み飛ばした後は子の失敗時の消費位置を返します()
    {
        var tested = new WithWhiteSpaceParser<char>(
            new CharParser('_'),
            new CallbackCharParser((_, _) => (false, '?', 1)));

        var result = tested.Parse(new Cursol("_あ!"));

        result.IsSuccess.Should().BeFalse();
        result.Cursol.Index.Should().Be(2);
        result.Cursol.Current.Should().Be('!');
    }
}
