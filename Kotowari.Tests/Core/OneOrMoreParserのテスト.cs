using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class OneOrMoreParserのテスト
{
    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(17)]
    [InlineData(65)]
    public void 参照型の列の順序と終端位置を保ち再解析後も繰り返し列挙できます(int length)
    {
        var text = new string(
            [.. from index in Enumerable.Range(0, length) select "abあ"[index % 3]]);
        var tested = new OneOrMoreParser<string>(
            new ParserConverter<char, string>(
                new ConditionalCharParser(char.IsLetter), it => it.ToString()));

        var result = tested.Parse(new Cursol(text + "!"));
        result.Parsed.Should().Equal(from character in text select character.ToString());
        tested.Parse(new Cursol(new string('z', length) + "!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Equal(from character in text select character.ToString());
        result.Cursol.Index.Should().Be(text.Length);
        result.Cursol.Current.Should().Be('!');
    }

    [Fact]
    public void 最初の要素が消費して失敗しても開始位置を返します()
    {
        var 呼び出し位置 = new List<int>();
        var tested = new OneOrMoreParser<char>(new CallbackCharParser((current, index) =>
        {
            呼び出し位置.Add(index);
            return (false, current, 1);
        }));
        var start = new Cursol("a!b").GoFoward(1);

        var result = tested.Parse(start);

        result.IsSuccess.Should().BeFalse();
        result.Cursol.Should().Be(start);
        呼び出し位置.Should().Equal(1);
    }

    [Fact]
    public void 後続の要素が消費して失敗した位置を引き継ぎ列挙では子を呼びません()
    {
        var 呼び出し位置 = new List<int>();
        var tested = new OneOrMoreParser<char>(new CallbackCharParser((current, index) =>
        {
            呼び出し位置.Add(index);
            return (current == 'a', 'A', 1);
        }));

        var result = tested.Parse(new Cursol("aa!b"));

        呼び出し位置.Should().Equal(0, 1, 2);
        result.IsSuccess.Should().BeTrue();
        result.Cursol.Index.Should().Be(3);
        result.Parsed.Should().Equal('A', 'A');
        result.Parsed.Should().Equal('A', 'A');
        呼び出し位置.Should().Equal(0, 1, 2);
    }

    [Fact]
    public void 長い列の途中の例外を伝播し同じパーサーで再解析できます()
    {
        var exception = new InvalidOperationException();
        var 例外を発生させる = true;
        var tested = new OneOrMoreParser<char>(new CallbackCharParser((current, index) =>
        {
            if (例外を発生させる && index == 64)
            {
                throw exception;
            }

            return (current == 'a', current, 1);
        }));
        var text = new string('a', 65);
        var parse = () => tested.Parse(new Cursol(text + "!"));

        parse.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
        例外を発生させる = false;

        tested.Parse(new Cursol(text + "!")).Parsed.Should().Equal(text);
    }

    [Fact]
    public void 一つ目の要素のパースに失敗したら失敗です()        {
        var tested = new OneOrMoreParser<char>(new CharParser('a'));

        var isSuccess = tested.Parse(new Cursol("b")).IsSuccess;

        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void 一つ目の要素のパースに成功したら成功です()
    {
        var tested = new OneOrMoreParser<char>(new CharParser('a'));

        var isSuccess = tested.Parse(new Cursol("a")).IsSuccess;

        isSuccess.Should().BeTrue();
    }
    [Fact]
    public void 一つ目の要素のパースに成功したら一つ目の要素のみが得られます()
    {
        var tested = new OneOrMoreParser<char>(new CharParser('a'));

        var parsed = tested.Parse(new Cursol("a")).Parsed;

        parsed.Should().ContainSingle();
        parsed.ElementAt(0).Should().Be('a');
    }

    [Fact]
    public void 一つ目の要素のパースに成功したらカーソルは進みます()
    {
        var tested = new OneOrMoreParser<char>(new CharParser('a'));

        var cursol = tested.Parse(new Cursol("a")).Cursol;

        cursol.Index.Should().Be("a".Length);
    }

    [Fact]
    public void 二つ目の要素のパースに成功したら二つ分カーソルは進みます()
    {
        var tested = new OneOrMoreParser<char>(new CharParser('a'));

        var cursol = tested.Parse(new Cursol("aa")).Cursol;

        cursol.Index.Should().Be("aa".Length);
    }

    [Fact]
    public void 二つ目の要素のパースに成功したら二つ目の要素もパースされます()
    {
        var tested = new OneOrMoreParser<char>(new CharParser('a'));

        var parsed = tested.Parse(new Cursol("aa")).Parsed;

        parsed.Count().Should().Be(2);
        parsed.ElementAt(0).Should().Be('a');
        parsed.ElementAt(1).Should().Be('a');
    }

    [Fact]
    public void 二つ目の要素のパースに失敗しても全体としては成功です()
    {
        var tested = new OneOrMoreParser<char>(new CharParser('a'));

        var isSuccess = tested.Parse(new Cursol("ab")).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void 二つ目の要素のパースに失敗したら一つ分カーソルは進みます()
    {
        var tested = new OneOrMoreParser<char>(new CharParser('a'));

        var cursol = tested.Parse(new Cursol("ab")).Cursol;

        cursol.Index.Should().Be("a".Length);
    }

    [Fact]
    public void 二つ目の要素のパースに失敗したらパース結果は一文字分です()
    {
        var tested = new OneOrMoreParser<char>(new CharParser('a'));

        var parsed = tested.Parse(new Cursol("ab")).Parsed;

        parsed.Should().ContainSingle();
        parsed.ElementAt(0).Should().Be('a');
    }
}
