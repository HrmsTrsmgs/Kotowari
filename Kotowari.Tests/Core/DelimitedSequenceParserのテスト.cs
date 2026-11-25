using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Linq;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class DelimitedSequenceParserのテスト
{
    [Fact]
    public void 要素と区切りのnull成功値を失敗と区別します()
    {
        var tested = new DelimitedSequenceParser<string?, string?>(
            new ParserConverter<char, string?>(
                new ConditionalCharParser(char.IsLetter), it => it == 'a' ? null : it.ToString()),
            ParserConverter.Create<string?, char>(new CharParser(','), _ => null));

        var result = tested.Parse(new Cursol("a,b!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Equal(null, "b");
        result.Cursol.Index.Should().Be(3);
    }

    [Fact]
    public void 要素と区切りの一巡で入力を消費せず成功したら例外を投げます()
    {
        var delimiterAttempts = 0;
        var tested = new DelimitedSequenceParser<char, char>(
            new CallbackCharParser((_, _) => (true, 'a', 0)),
            new CallbackCharParser((_, _) =>
                ++delimiterAttempts == 1
                    ? (true, ',', 0)
                    : throw new NotSupportedException("反復が停止しませんでした")));
        var parse = () => tested.Parse(new Cursol("a"));

        parse.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void 要素が空でも区切りが入力を消費すれば反復を続けます()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CallbackCharParser((_, _) => (true, 'a', 0)),
            new CharParser(','));

        var result = tested.Parse(new Cursol(",!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Equal('a', 'a');
        result.Cursol.Index.Should().Be(1);
    }

    [Fact]
    public void 区切りが空でも要素が入力を消費すれば反復を続けます()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CharParser('a'),
            new CallbackCharParser((_, _) => (true, ',', 0)));

        var result = tested.Parse(new Cursol("aa!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Equal('a', 'a');
        result.Cursol.Index.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(17)]
    [InlineData(65)]
    public void 参照型の列の順序と終端位置を保ち再解析後も繰り返し列挙できます(int length)
    {
        var values = from index in Enumerable.Range(0, length) select "abあ"[index % 3].ToString();
        var text = string.Join(",", values);
        var tested = new DelimitedSequenceParser<string, char>(
            new ParserConverter<char, string>(
                new ConditionalCharParser(char.IsLetter), it => it.ToString()),
            new CharParser(','));

        var result = tested.Parse(new Cursol(text + ",!"));
        result.Parsed.Should().Equal(values);
        tested.Parse(new Cursol("z,z!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Equal(values);
        result.Cursol.Index.Should().Be(text.Length);
        result.Cursol.Current.Should().Be(',');
    }

    [Theory]
    [InlineData("a,!")]
    [InlineData("a!")]
    public void 要素や区切り子が消費して失敗しても最後の要素の直後を返します(string input)
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CallbackCharParser((current, _) => (current == 'a', 'A', 1)),
            new CallbackCharParser((current, _) => (current == ',', current, 1)));

        var result = tested.Parse(new Cursol(input));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Equal('A');
        result.Cursol.Index.Should().Be(1);
    }

    [Fact]
    public void 長い列の途中の例外を伝播し同じパーサーで再解析できます()
    {
        var exception = new InvalidOperationException();
        var 例外を発生させる = true;
        var tested = new DelimitedSequenceParser<char, char>(
            new CallbackCharParser((current, index) =>
            {
                if (例外を発生させる && index == 128)
                {
                    throw exception;
                }

                return (current == 'a', current, 1);
            }),
            new CharParser(','));
        var text = string.Join(",", Enumerable.Repeat("a", 65));
        var parse = () => tested.Parse(new Cursol(text + "!"));

        parse.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
        例外を発生させる = false;

        tested.Parse(new Cursol(text + "!")).Parsed.Should().Equal(Enumerable.Repeat('a', 65));
    }

    [Fact]
    public void 最初の要素のパースに失敗しても成功です()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CharParser('a'),
            new CharParser(','));

        tested.Parse(new Cursol("b")).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void 最初の要素のパースに失敗すればカーソルは進みません()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CharParser('a'),
            new CharParser(','));

        var cursol = tested.Parse(new Cursol("b")).Cursol;

        cursol.Index.Should().Be(0);
    }

    [Fact]
    public void 最初の要素のパースに失敗すれば空の要素が結果となります()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CharParser('a'),
            new CharParser(','));

        tested.Parse(new Cursol("b")).Parsed.Should().BeEmpty();
    }

    [Fact]
    public void 最初の要素のパースに成功すれば成功です()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CharParser('a'),
            new CharParser(','));

        tested.Parse(new Cursol("a")).IsSuccess.Should().BeTrue();
    }
    [Fact]
    public void 最初の要素のパースに成功すればカーソルが進みます()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CharParser('a'),
            new CharParser(','));

        var cursol = tested.Parse(new Cursol("a")).Cursol;

        cursol.Index.Should().Be("a".Length);
    }
    [Fact]
    public void 最初の要素のパースに成功すれば最初の一つが結果になります()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CharParser('a'),
            new CharParser(','));

        tested.Parse(new Cursol("a")).Parsed.Should().Equal('a');
    }
    [Fact]
    public void 区切り子で終わった場合は区切り子は読み込みません()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CharParser('a'),
            new CharParser(','));

        var cursol = tested.Parse(new Cursol("a,")).Cursol;

        cursol.Index.Should().Be("a".Length);
    }

    [Fact]
    public void 二つ目の要素も結果に入ります()
    {
        var tested = new DelimitedSequenceParser<char, char>(
            new CharParser('a'),
            new CharParser(','));

        tested.Parse(new Cursol("a,a")).Parsed.Should().Equal('a', 'a');
    }
}
