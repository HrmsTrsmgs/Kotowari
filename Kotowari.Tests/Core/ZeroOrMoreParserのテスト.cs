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
