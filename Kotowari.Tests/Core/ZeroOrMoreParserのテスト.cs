using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class ZeroOrMoreParserのテスト
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(65)]
    [InlineData(257)]
    public void UTF16とUTF8で列の順序と終端位置を保ち再解析しても結果は変わりません(int length)
    {
        var text = new string(
            [.. from index in Enumerable.Range(0, length) select "abあ"[index % 3]]);
        var tested = new ZeroOrMoreParser<char>(
            new OrParser<char>(new CharParser('a'), new CharParser('b'), new CharParser('あ')));

        var utf16 = tested.Parse(Cursol.FromString(text + "!"));
        var utf8 = tested.Parse(Cursol.FromUtf8(Encoding.UTF8.GetBytes(text + "!")));
        tested.Parse(Cursol.FromString(new string('b', length) + "!"));
        tested.Parse(Cursol.FromUtf8(Encoding.UTF8.GetBytes(new string('a', length) + "!")));

        utf16.Parsed.Should().Equal(text);
        utf16.Cursol.Index.Should().Be(text.Length);
        utf16.Cursol.Current.Should().Be('!');
        utf8.Parsed.Should().Equal(text);
        utf8.Cursol.Index.Should().Be(Encoding.UTF8.GetByteCount(text));
        utf8.Cursol.Current.Should().Be('!');
        utf16.Parsed.Should().Equal(text);
        utf8.Parsed.Should().Equal(text);
    }

    [Fact]
    public void 参照型の列も解析後に繰り返し列挙でき別の解析結果と混ざりません()
    {
        var tested = new ZeroOrMoreParser<string>(
            new ParserConverter<char, string>(
                new ConditionalCharParser(char.IsLetter),
                it => it.ToString()));
        var text = string.Concat(Enumerable.Repeat("abあ", 100));

        var utf16 = tested.Parse(Cursol.FromString(text + "!"));
        var utf8 = tested.Parse(Cursol.FromUtf8(Encoding.UTF8.GetBytes(text + "!")));
        tested.Parse(Cursol.FromString(new string('z', text.Length) + "!"));

        utf16.Parsed.Should().Equal(from character in text select character.ToString());
        utf8.Parsed.Should().Equal(from character in text select character.ToString());
        utf16.Parsed.Should().Equal(from character in text select character.ToString());
        utf8.Parsed.Should().Equal(from character in text select character.ToString());
    }

    [Fact]
    public void 長い列の途中の例外を伝播し同じパーサーで再解析できます()
    {
        var exception = new InvalidOperationException();
        var 例外を発生させる = true;
        var tested = new ZeroOrMoreParser<char>(new CallbackCharParser((current, index) =>
        {
            if (例外を発生させる && index == 65)
            {
                throw exception;
            }

            return (current == 'a', current, 1);
        }));
        var text = new string('a', 100);
        Action parse = () => tested.Parse(Cursol.FromString(text + "!"));

        parse.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
        例外を発生させる = false;

        tested.Parse(Cursol.FromString(text + "!")).Parsed.Should().Equal(text);
        tested.Parse(Cursol.FromUtf8(Encoding.UTF8.GetBytes(text + "!"))).Parsed.Should().Equal(text);
    }

    [Fact]
    public void 一つ目の要素のパースに失敗しても成功です()        {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var isSuccess = tested.Parse(Cursol.FromString("b")).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void 一つ目の要素のパースに失敗したらカーソルは進みません()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var cursol = tested.Parse(Cursol.FromString("b")).Cursol;

        cursol.Index.Should().Be(0);
    }

    [Fact]
    public void 一つ目の要素のパースに失敗したら結果は空です()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var parsed = tested.Parse(Cursol.FromString("b")).Parsed;

        parsed.Should().BeEmpty();
    }

    [Fact]
    public void 一つ目の要素のパースに成功したら成功です()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var isSuccess = tested.Parse(Cursol.FromString("a")).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void 一つ目の要素のパースに成功したらカーソルは進みます()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var cursol = tested.Parse(Cursol.FromString("a")).Cursol;

        cursol.Index.Should().Be("a".Length);
    }

    [Fact]
    public void 一つ目の要素のパースに成功したら一つ目の要素のみが得られます()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var parsed = tested.Parse(Cursol.FromString("a")).Parsed;

        parsed.Should().ContainSingle();
        parsed.ElementAt(0).Should().Be('a');
    }

    [Fact]
    public void 二つ目の要素のパースに成功したら二つ分カーソルは進みます()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var cursol = tested.Parse(Cursol.FromString("aa")).Cursol;

        cursol.Index.Should().Be("aa".Length);
    }

    [Fact]
    public void 二つ目の要素のパースに成功したら二つ目の要素もパースされます()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var parsed = tested.Parse(Cursol.FromString("aa")).Parsed;

        parsed.Count().Should().Be(2);
        parsed.ElementAt(0).Should().Be('a');
        parsed.ElementAt(1).Should().Be('a');
    }

    [Fact]
    public void 外部派生パーサーの成功値と失敗時の消費位置を引き継ぎます()
    {
        var 呼び出し位置 = new List<int>();
        var tested = new ZeroOrMoreParser<char>(new CallbackCharParser((current, index) =>
        {
            呼び出し位置.Add(index);
            return (current == 'a', 'A', 1);
        }));

        var parsed = tested.Parse(Cursol.FromString("a!"));

        parsed.IsSuccess.Should().BeTrue();
        parsed.Parsed.Should().Equal('A');
        parsed.Cursol.Index.Should().Be(2);
        呼び出し位置.Should().Equal(0, 1);
    }

    [Fact]
    public void 外部派生パーサーの例外をそのまま伝播します()
    {
        var exception = new InvalidOperationException();
        var tested = new ZeroOrMoreParser<char>(new CallbackCharParser((_, _) => throw exception));

        Action parse = () => tested.Parse(Cursol.FromString("a"));

        parse.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
    }

    [Fact]
    public void 組み合わせから呼んでも派生反復パーサーの処理と基底実装を使います()
    {
        var parser = new UpperCaseZeroOrMoreParser();
        var tested = new OrParser<IEnumerable<char>>(parser);

        var utf16 = tested.Parse(Cursol.FromString("aa!"));
        var utf8 = tested.Parse(Cursol.FromUtf8("aa!"u8));

        utf16.Parsed.Should().Equal('A', 'A');
        utf16.Cursol.Index.Should().Be(2);
        utf8.Parsed.Should().Equal('A', 'A');
        utf8.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(2);
    }
}
