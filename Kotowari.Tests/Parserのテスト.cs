using FluentAssertions;
using Marimo.Kotowari.Core;
using Marimo.Kotowari.Tests.Core;
using System;
using System.Collections.Generic;
using Xunit;

namespace Marimo.Kotowari.Tests;

public class Parserのテスト
{
    [Fact]
    public void 派生パーサーは異なる結果型の子を解析して値と消費位置を返せます()
    {
        Parser<int> tested = new ConvertingChildParser<string, int>(new WordParser("abc"), it => it.Length);
        var start = new Cursol("abc!");

        ParseResult<int> result = tested.Parse(start);

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(3);
        (result.Cursol == start.GoFoward(3)).Should().BeTrue();
    }

    [Theory]
    [InlineData("a!", true)]
    [InlineData("b!", false)]
    public void 派生パーサーは子のnull成功と消費した失敗を戻り値で区別できます(string input, bool isSuccess)
    {
        Parser<string?> tested = new ConvertingChildParser<string?, string?>(new NullStringParser(), it => it);
        var start = new Cursol(input);

        ParseResult<string?> result = tested.Parse(start);

        result.IsSuccess.Should().Be(isSuccess);
        (result.Cursol == start.GoFoward(1)).Should().BeTrue();
        if (isSuccess)
        {
            result.Parsed.Should().BeNull();
        }
        else
        {
            var readParsed = () => result.Parsed;

            readParsed.Should().Throw<InvalidOperationException>();
        }
    }

    [Fact]
    public void 子と後置空白がnullで成功しても子の値と空白の消費を保ちます()
    {
        var tested = ParserConverter.Create<string?, char>(new CharParser('a'), _ => null)
            .WithWhiteSpace(ParserConverter.Create<string?, char>(new CharParser('_'), _ => null));

        var result = tested.Parse(new Cursol("a__!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().BeNull();
        result.Cursol.Index.Should().Be(3);
    }

    [Fact]
    public void 親に指定した空白規則を未指定の子が引き継ぎます()
    {
        var tested = SequenceParser.Create(
            new WordParser("public"),
            new WordParser("static"))
            .WithWhiteSpace(new CharParser('_'));

        var result = tested.Parse(new Cursol("public__static_!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("public", "static"));
        result.Cursol.Index.Should().Be(15);
        result.Cursol.Current.Should().Be('!');
    }

    [Fact]
    public void 子の後の空白パーサーが消費して失敗しても最後に成功した位置を返します()
    {
        var 呼び出し位置 = new List<int>();
        var tested = new CharParser('あ').WithWhiteSpace(new CallbackCharParser((current, index) =>
        {
            呼び出し位置.Add(index);
            return (current == '_', current, 1);
        }));

        var result = tested.Parse(new Cursol("あ__!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be('あ');
        result.Cursol.Index.Should().Be(3);
        result.Cursol.Current.Should().Be('!');
        呼び出し位置.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void 後置空白が初回に消費して失敗したら子の直後の位置を返します()
    {
        var 呼び出し位置 = new List<int>();
        var tested = new CharParser('あ').WithWhiteSpace(new CallbackCharParser((_, index) =>
        {
            呼び出し位置.Add(index);
            return (false, '?', 1);
        }));

        var result = tested.Parse(new Cursol("あ!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be('あ');
        result.Cursol.Index.Should().Be(1);
        result.Cursol.Current.Should().Be('!');
        呼び出し位置.Should().Equal(1);
    }

    [Fact]
    public void 後置空白の例外はそのまま伝播します()
    {
        var exception = new InvalidOperationException();
        var tested = new CharParser('あ').WithWhiteSpace(new CallbackCharParser((_, _) => throw exception));

        var parse = () => tested.Parse(new Cursol("あ!"));

        parse.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
    }

    [Fact]
    public void 文字列を返す複数文字の空白規則を繰り返し読み飛ばします()
    {
        var tested = new WordParser("public").WithWhiteSpace(new WordParser("__"));

        var result = tested.Parse(new Cursol("public____!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("public");
        result.Cursol.Index.Should().Be(10);
        result.Cursol.Current.Should().Be('!');
    }

    [Fact]
    public void 子の前にある空白は読み飛ばしません()
    {
        var tested = new CharParser('あ').WithWhiteSpace(new CharParser('_'));

        tested.Parse(new Cursol("_あ")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void 子の値を返して文法で指定した後置空白だけを読み飛ばします()
    {
        var tested = new WordParser("public").WithWhiteSpace(new CharParser('_'));

        var result = tested.Parse(new Cursol("public__ !"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("public");
        result.Cursol.Index.Should().Be(8);
        result.Cursol.Current.Should().Be(' ');
    }

    [Fact]
    public void 大文字小文字を無視する子の値を返して指定した後置空白を読み飛ばします()
    {
        var tested = new WordParser("public", true).WithWhiteSpace(new CharParser('_'));

        var result = tested.Parse(new Cursol("PUBLIC_"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("PUBLIC");
        result.Cursol.Index.Should().Be(7);
    }

    [Fact]
    public void 子が空の文字列で成功しても指定した後置空白を読み飛ばします()
    {
        var tested = new WordParser("").WithWhiteSpace(new CharParser('_'));

        var result = tested.Parse(new Cursol("__x"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().BeEmpty();
        result.Cursol.Index.Should().Be(2);
    }

    [Fact]
    public void 子が入力を消費して失敗したら後置空白は読みません()
    {
        var 空白の呼び出し回数 = 0;
        var tested = new CallbackCharParser((_, _) => (false, '?', 1))
            .WithWhiteSpace(new CallbackCharParser((current, _) =>
            {
                ++空白の呼び出し回数;
                return (current == '_', current, 1);
            }));

        var result = tested.Parse(new Cursol("あ_!"));

        result.IsSuccess.Should().BeFalse();
        空白の呼び出し回数.Should().Be(0);
    }

    [Theory]
    [InlineData("public-static_!", true, 14)]
    [InlineData("public-_static_!", false, 0)]
    public void 子が明示した空白規則は親の規則より優先されます(string input, bool isSuccess, int index)
    {
        var tested = SequenceParser.Create(
            new WordParser("public").WithWhiteSpace(new CharParser('-')),
            new WordParser("static"))
            .WithWhiteSpace(new CharParser('_'));

        var result = tested.Parse(new Cursol(input));

        result.IsSuccess.Should().Be(isSuccess);
        result.Cursol.Index.Should().Be(index);
        if (isSuccess)
        {
            result.Parsed.Should().Be(("public", "static"));
            result.Cursol.Current.Should().Be('!');
        }
    }

    [Fact]
    public void 入れ子の未指定の子は最も近い親の空白規則を引き継ぎます()
    {
        var nested = SequenceParser.Create(new WordParser("public"), new WordParser("static"))
            .WithWhiteSpace(new CharParser('_'));
        var tested = SequenceParser.Create(nested, new WordParser("int"))
            .WithWhiteSpace(new CharParser('-'));

        var result = tested.Parse(new Cursol("public_static_int-!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be((("public", "static"), "int"));
        result.Cursol.Index.Should().Be(18);
        result.Cursol.Current.Should().Be('!');
    }

    [Fact]
    public void 空白規則の再指定は後の指定を使い元のパーサーと以前の設定版を変えません()
    {
        var original = new WordParser("public");
        var previous = original.WithWhiteSpace(new CharParser('_'));
        var tested = previous.WithWhiteSpace(new CharParser('-'));

        var latest = tested.Parse(new Cursol("public-_!"));
        var withoutPrevious = tested.Parse(new Cursol("public_-!"));
        var previousResult = previous.Parse(new Cursol("public_-!"));
        var originalResult = original.Parse(new Cursol("public_!"));

        latest.IsSuccess.Should().BeTrue();
        latest.Parsed.Should().Be("public");
        latest.Cursol.Index.Should().Be(7);
        latest.Cursol.Current.Should().Be('_');
        withoutPrevious.IsSuccess.Should().BeTrue();
        withoutPrevious.Parsed.Should().Be("public");
        withoutPrevious.Cursol.Index.Should().Be(6);
        withoutPrevious.Cursol.Current.Should().Be('_');
        previousResult.IsSuccess.Should().BeTrue();
        previousResult.Parsed.Should().Be("public");
        previousResult.Cursol.Index.Should().Be(7);
        previousResult.Cursol.Current.Should().Be('-');
        originalResult.IsSuccess.Should().BeTrue();
        originalResult.Parsed.Should().Be("public");
        originalResult.Cursol.Index.Should().Be(6);
        originalResult.Cursol.Current.Should().Be('_');
    }

    [Fact(Skip = "[空白責務分離 099] 組み合わせたパーサーの空白規則の再指定時に以前の規則が子に残るため。後の規則だけを使う実装後に解除します。")]
    public void 組み合わせたパーサーの空白規則の再指定は子にも後の規則を使います()
    {
        var tested = SequenceParser.Create(new WordParser("public"), new WordParser("static"))
            .WithWhiteSpace(new CharParser('_'))
            .WithWhiteSpace(new CharParser('-'));

        var result = tested.Parse(new Cursol("public-static-!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("public", "static"));
        result.Cursol.Current.Should().Be('!');
    }

    [Fact]
    public void 共有する子を異なる空白規則の親で再利用しても他の利用箇所を変えません()
    {
        var shared = new WordParser("public");
        var underscores = SequenceParser.Create(shared, new WordParser("static"))
            .WithWhiteSpace(new CharParser('_'));
        var hyphens = SequenceParser.Create(shared, new WordParser("static"))
            .WithWhiteSpace(new CharParser('-'));

        ParseResult<(string, string)>[] parentResults =
        [
            underscores.Parse(new Cursol("public_static_!")),
            hyphens.Parse(new Cursol("public-static-!")),
            underscores.Parse(new Cursol("public_static_!"))
        ];
        var original = shared.Parse(new Cursol("public_!"));

        parentResults.Should().AllSatisfy(result =>
        {
            result.IsSuccess.Should().BeTrue();
            result.Parsed.Should().Be(("public", "static"));
            result.Cursol.Index.Should().Be(14);
            result.Cursol.Current.Should().Be('!');
        });

        original.IsSuccess.Should().BeTrue();
        original.Parsed.Should().Be("public");
        original.Cursol.Index.Should().Be(6);
        original.Cursol.Current.Should().Be('_');
    }

    [Theory]
    [InlineData("public_static", true, 14)]
    [InlineData("publicstatic", false, 0)]
    public void 空白規則は単語の文字列内部に適用されません(string word, bool expectedSuccess, int expectedIndex)
    {
        var tested = new WordParser(word).WithWhiteSpace(new CharParser('_'));

        var result = tested.Parse(new Cursol("public_static_!"));

        result.IsSuccess.Should().Be(expectedSuccess);
        result.Cursol.Index.Should().Be(expectedIndex);
        if (expectedSuccess)
        {
            result.Parsed.Should().Be("public_static");
            result.Cursol.Current.Should().Be('!');
        }
    }

    [Fact]
    public void 空白が入力を消費せず成功したら例外を投げます()
    {
        var attempts = 0;
        var 消費しない空白 = new CallbackCharParser((_, _) =>
        {
            if (++attempts > 1)
            {
                throw new NotSupportedException("反復が停止しませんでした");
            }

            return (IsSuccess: true, Parsed: ' ', Step: 0);
        });
        var tested = new CharParser('a').WithWhiteSpace(消費しない空白);
        var parse = () => tested.Parse(new Cursol("a"));

        parse.Should().Throw<InvalidOperationException>();
    }
}
