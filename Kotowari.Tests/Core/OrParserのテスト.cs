using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class OrParserのテスト
{
    [Fact]
    public void 一つ目の要素のパースに成功したら成功です()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var isSuccess = tested.Parse(Cursol.FromString("abc")).IsSuccess;

        isSuccess.Should().BeTrue();
    }


    [Fact]
    public void 一つ目の要素のパースに成功したら一つ目の要素が返ります()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var parsed = tested.Parse(Cursol.FromString("abc")).Parsed;

        parsed.Should().Be("abc");
    }

    [Fact]
    public void 一つ目の要素のパースに成功したら一つ目の要素の分だけカーソルが進みます()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var cursol = tested.Parse(Cursol.FromString("abc")).Cursol;

        cursol.Index.Should().Be("abc".Length);
   }

    [Fact]
    public void 一つ目の要素のパースに失敗し二つ目の要素には成功したら成功です()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var isSuccess = tested.Parse(Cursol.FromString("ab")).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void 一つ目の要素のパースに失敗し二つ目の要素のパースに成功したら二つ目の要素が返ります()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var parsed = tested.Parse(Cursol.FromString("abc")).Parsed;

        parsed.Should().Be("abc");
    }

    [Fact]
    public void 一つ目の要素のパースに失敗し二つ目の要素には成功したら二つ目の要素の分だけカーソルが進みます()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var cursol = tested.Parse(Cursol.FromString("ab")).Cursol;

        cursol.Index.Should().Be("ab".Length);
    }

    [Fact]
    public void 二つとも失敗ならパースは失敗です()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var isSuccess = tested.Parse(Cursol.FromString("a")).IsSuccess;

        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void 二つとも失敗ならカーソルは進みません()
    {
        var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

        var cursol = tested.Parse(Cursol.FromString("a")).Cursol;

        cursol.Index.Should().Be(0);
    }

    [Fact]
    public void UTF8入力でシーケンスの途中失敗後も次の候補を開始位置から読めます()
    {
        var tested = new OrParser<(char, char)>(
            SequenceParser.Create(new CharParser('あ'), new CharParser('x')),
            SequenceParser.Create(new CharParser('あ'), new CharParser('é')));
        var utf16Start = Cursol.FromString("前あéb").GoFoward(1);
        var utf8Start = Cursol.FromUtf8(Encoding.UTF8.GetBytes("前あéb")).GoFoward(1);

        var utf16 = tested.Parse(utf16Start);
        var utf8 = tested.Parse(utf8Start);

        utf16.IsSuccess.Should().BeTrue();
        utf16.Parsed.Should().Be(('あ', 'é'));
        utf16.Cursol.Index.Should().Be(3);
        utf8.IsSuccess.Should().BeTrue();
        utf8.Parsed.Should().Be(utf16.Parsed);
        (utf8.Cursol == utf8Start.GoFoward(2)).Should().BeTrue();
        utf8.Cursol.Index.Should().Be(8);
        utf8.Cursol.Current.Should().Be('b');
    }

    [Fact]
    public void UTF8入力で全シーケンスが途中失敗すると元の文書と開始位置を返します()
    {
        var tested = new OrParser<(char, char)>(
            SequenceParser.Create(new CharParser('あ'), new CharParser('x')),
            SequenceParser.Create(new CharParser('あ'), new CharParser('é')));
        var utf16Start = Cursol.FromString("前あいb").GoFoward(1);
        var utf8Start = Cursol.FromUtf8(Encoding.UTF8.GetBytes("前あいb")).GoFoward(1);

        var utf16 = tested.Parse(utf16Start);
        var utf8 = tested.Parse(utf8Start);

        utf16.IsSuccess.Should().BeFalse();
        (utf16.Cursol == utf16Start).Should().BeTrue();
        utf8.IsSuccess.Should().BeFalse();
        (utf8.Cursol == utf8Start).Should().BeTrue();
        utf8.Cursol.Index.Should().Be(3);
        utf8.Cursol.Current.Should().Be('あ');
    }

    [Fact]
    public void 渡した候補配列の差し替えは解析後も両入力へ反映されます()
    {
        Parser<char>[] parsers = [new CharParser('a')];
        var tested = new ZeroOrMoreParser<char>(new OrParser<char>(parsers));

        tested.Parse(Cursol.FromString("a!")).Parsed.Should().Equal('a');
        tested.Parse(Cursol.FromUtf8("a!"u8)).Parsed.Should().Equal('a');
        parsers[0] = new CharParser('あ');

        var utf16 = tested.Parse(Cursol.FromString("あ!"));
        var utf8 = tested.Parse(Cursol.FromUtf8("あ!"u8));

        utf16.Parsed.Should().Equal('あ');
        utf16.Cursol.Index.Should().Be(1);
        utf8.Parsed.Should().Equal('あ');
        utf8.Cursol.Index.Should().Be(3);
    }

    [Fact]
    public void 後の候補の子がnullでも到達するまで例外を出しません()
    {
        var tested = new OrParser<IEnumerable<char>>(
            new OneOrMoreParser<char>(new CharParser('a')),
            new ZeroOrMoreParser<char>(null!));

        tested.Parse(Cursol.FromString("a!")).Parsed.Should().Equal('a');
        tested.Parse(Cursol.FromUtf8("a!"u8)).Parsed.Should().Equal('a');

        Action parseUtf16 = () => tested.Parse(Cursol.FromString("b"));
        Action parseUtf8 = () => tested.Parse(Cursol.FromUtf8("b"u8));

        parseUtf16.Should().Throw<NullReferenceException>();
        parseUtf8.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void 組み合わせから呼んでも派生選択パーサーの処理と基底実装を使います()
    {
        var parser = new UpperCaseOrParser();
        var tested = new ZeroOrMoreParser<char>(parser);

        var utf16 = tested.Parse(Cursol.FromString("ab!"));
        var utf8 = tested.Parse(Cursol.FromUtf8("ab!"u8));

        utf16.Parsed.Should().Equal('A', 'B');
        utf16.Cursol.Index.Should().Be(2);
        utf8.Parsed.Should().Equal('A', 'B');
        utf8.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(6);
    }
}
