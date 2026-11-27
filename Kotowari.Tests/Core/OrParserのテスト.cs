using FluentAssertions;
using Marimo.Kotowari.Core;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class OrParserのテスト
{
    [Fact]
    public void 一つ目の要素がnullで成功したら失敗と区別してその値を返します()
    {
        var tested = new OrParser<string?>(
            ParserConverter.Create<string?, string>(new WordParser("abc"), _ => null),
            ParserConverter.Create<string?, string>(new WordParser("abc"), _ => "fallback"));

        var result = tested.Parse(new Cursol("abc!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().BeNull();
        result.Cursol.Index.Should().Be(3);
    }

    [Fact]
    public void 一つ目の要素のパースに成功したら成功です()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        tested.Parse(new Cursol("abc")).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void 一つ目の要素のパースに成功したら後続候補を試さず一つ目の要素を返します()
    {
        var secondWasTried = false;
        var tested = new OrParser<string>(
            new WordParser("abc"),
            new DebugParser<string>(new WordParser("ab"), () => secondWasTried = true));

        tested.Parse(new Cursol("abc")).Parsed.Should().Be("abc");
        secondWasTried.Should().BeFalse();
    }

    [Fact]
    public void 一つ目の要素のパースに成功したら一つ目の要素の分だけカーソルが進みます()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var cursol = tested.Parse(new Cursol("abc")).Cursol;

        cursol.Index.Should().Be("abc".Length);
    }

    [Fact]
    public void 一つ目の要素のパースに失敗し二つ目の要素には成功したら成功です()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        tested.Parse(new Cursol("ab")).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void 一つ目の要素のパースに失敗し二つ目の要素のパースに成功したら二つ目の要素が返ります()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        tested.Parse(new Cursol("ab")).Parsed.Should().Be("ab");
    }

    [Fact]
    public void 一つ目の要素のパースに失敗し二つ目の要素には成功したら二つ目の要素の分だけカーソルが進みます()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var cursol = tested.Parse(new Cursol("ab")).Cursol;

        cursol.Index.Should().Be("ab".Length);
    }

    [Fact]
    public void 二つとも失敗ならパースは失敗です()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        tested.Parse(new Cursol("a")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void 二つとも失敗ならカーソルは進みません()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var cursol = tested.Parse(new Cursol("a")).Cursol;

        cursol.Index.Should().Be(0);
    }

    [Fact]
    public void 文字列入力でシーケンスの途中失敗後も次の候補を開始位置から読めます()
    {
        var tested = new OrParser<(char, char)>(
            SequenceParser.Create(new CharParser('あ'), new CharParser('x')),
            SequenceParser.Create(new CharParser('あ'), new CharParser('é')));
        var start = new Cursol("前あéb").GoFoward(1);

        var result = tested.Parse(start);

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(('あ', 'é'));
        result.Cursol.Index.Should().Be(3);
    }

    [Fact]
    public void 文字列入力で全シーケンスが途中失敗すると元の文書と開始位置を返します()
    {
        var tested = new OrParser<(char, char)>(
            SequenceParser.Create(new CharParser('あ'), new CharParser('x')),
            SequenceParser.Create(new CharParser('あ'), new CharParser('é')));
        var start = new Cursol("前あいb").GoFoward(1);

        var result = tested.Parse(start);

        result.IsSuccess.Should().BeFalse();
        (result.Cursol == start).Should().BeTrue();
    }

    [Fact]
    public void 渡した候補配列の差し替えは解析後も文字列入力へ反映されます()
    {
        Parser<char>[] parsers = [new CharParser('a')];
        var tested = new ZeroOrMoreParser<char>(new OrParser<char>(parsers));

        tested.Parse(new Cursol("a!")).Parsed.Should().Equal('a');
        parsers[0] = new CharParser('あ');

        var result = tested.Parse(new Cursol("あ!"));

        result.Parsed.Should().Equal('あ');
        result.Cursol.Index.Should().Be(1);
    }

    [Fact]
    public void 組み合わせから呼んでも派生選択パーサーの処理と基底実装を使います()
    {
        var parser = new UpperCaseOrParser();
        var tested = new ZeroOrMoreParser<char>(parser);

        var result = tested.Parse(new Cursol("ab!"));

        result.Parsed.Should().Equal('A', 'B');
        result.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(3);
    }
}
