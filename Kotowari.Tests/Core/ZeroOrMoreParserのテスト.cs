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

        var isSuccess = tested.Parse(new Cursol("b")).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void 一つ目の要素のパースに失敗したらカーソルは進みません()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var cursol = tested.Parse(new Cursol("b")).Cursol;

        cursol.Index.Should().Be(0);
    }

    [Fact]
    public void 一つ目の要素のパースに失敗したら結果は空です()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var parsed = tested.Parse(new Cursol("b")).Parsed;

        parsed.Should().BeEmpty();
    }

    [Fact]
    public void 一つ目の要素のパースに成功したら成功です()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var isSuccess = tested.Parse(new Cursol("a")).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void 一つ目の要素のパースに成功したらカーソルは進みます()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var cursol = tested.Parse(new Cursol("a")).Cursol;

        cursol.Index.Should().Be("a".Length);
    }

    [Fact]
    public void 一つ目の要素のパースに成功したら一つ目の要素のみが得られます()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var parsed = tested.Parse(new Cursol("a")).Parsed;

        parsed.Should().ContainSingle();
        parsed.ElementAt(0).Should().Be('a');
    }

    [Fact]
    public void 二つ目の要素のパースに成功したら二つ分カーソルは進みます()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var cursol = tested.Parse(new Cursol("aa")).Cursol;

        cursol.Index.Should().Be("aa".Length);
    }

    [Fact]
    public void 二つ目の要素のパースに成功したら二つ目の要素もパースされます()
    {
        var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

        var parsed = tested.Parse(new Cursol("aa")).Parsed;

        parsed.Count().Should().Be(2);
        parsed.ElementAt(0).Should().Be('a');
        parsed.ElementAt(1).Should().Be('a');
    }

    [Fact]
    public void 外部派生パーサーの成功値と失敗時の消費位置を引き継ぎます()
    {
        var 呼び出し位置 = new List<int>();
        var tested = new ZeroOrMoreParser<char>(new 外部派生パーサー(it =>
        {
            呼び出し位置.Add(it.Index);
            return it.Current == 'a'
                ? ParseResult<char>.Success(it.GoFoward(1), 'A')
                : ParseResult<char>.Failure(it.GoFoward(1));
        }));

        var parsed = tested.Parse(new Cursol("a!"));

        parsed.IsSuccess.Should().BeTrue();
        parsed.Parsed.Should().Equal('A');
        parsed.Cursol.Index.Should().Be(2);
        呼び出し位置.Should().Equal(0, 1);
    }

    [Fact]
    public void 外部派生パーサーの例外をそのまま伝播します()
    {
        var exception = new InvalidOperationException();
        var tested = new ZeroOrMoreParser<char>(new 外部派生パーサー(_ => throw exception));

        var parse = () => tested.Parse(new Cursol("a"));

        parse.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
    }

    sealed class 外部派生パーサー(Func<Cursol, ParseResult<char>> parse) : Parser<char>
    {
        protected override ParseResult<char> ParseCore(Cursol cursol)
            => parse(cursol);
    }
}
