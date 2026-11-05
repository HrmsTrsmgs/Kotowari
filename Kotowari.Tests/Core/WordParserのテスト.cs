using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class WordParserのテスト
{
    [Fact]
    public void 空白が入力を消費せず成功したら例外を投げます()
    {
        var attempts = 0;
        var tested = new WordParser("a", new CallbackCharParser((_, _) =>
            ++attempts == 1
                ? (true, ' ', 0)
                : throw new NotSupportedException("反復が停止しませんでした")));
        var parse = () => tested.Parse(new Cursol("a"));

        parse.Should().Throw<InvalidOperationException>();
    }


    [Fact]
    public void ParseAsyncは指定した単語を読み込みに成功します()
    {
        var cursol = new Cursol("public");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncは指定していない単語を読み込みに失敗します()
    {
        var cursol = new Cursol("publi");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }
    [Fact]
    public void ParseAsyncは指定した単語を読み込みます()
    {
        var cursol = new Cursol("public");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.Parsed.Should().Be("public");
    }

    [Fact]
    public void ParseAsyncは読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
    {
        var cursol = new Cursol("void");
        var tested = new WordParser("void");

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(4);
    }

    [Fact]
    public void ParseAsyncは読み込みに失敗した場合には進んでいないカーソルを返します()
    {
        var cursol = new Cursol("publi");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(0);
    }

    [Fact(Skip = "WordParserが前置空白を暗黙に読んでいるため。指定文字列だけを照合する実装後に解除します。")]
    public void 指定文字列の前に空白があると一致せず入力を消費しません()
    {
        var cursol = new Cursol(" public");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
        result.Cursol.Should().Be(cursol);
    }

    [Fact(Skip = "WordParserが後置空白を暗黙に読んでいるため。指定文字列だけを照合する実装後に解除します。")]
    public void 指定文字列に一致しても後ろの空白は消費しません()
    {
        var cursol = new Cursol("public ");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("public");
        result.Cursol.Index.Should().Be(6);
        result.Cursol.Current.Should().Be(' ');
    }

    [Fact(Skip = "WordParserが指定文字列の照合前に空白を読んでいるため。文字列内の空白もそのまま照合する実装後に解除します。")]
    public void 指定文字列に含まれる前後の空白もそのまま照合します()
    {
        var tested = new WordParser(" public ");

        var result = tested.Parse(new Cursol(" public !"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(" public ");
        result.Cursol.Index.Should().Be(8);
        result.Cursol.Current.Should().Be('!');
    }

    [Fact]
    public void 指定した空白パーサーで単語前後を読み飛ばします()
    {
        var tested = new WordParser("public", new CharParser('_'));

        var result = tested.Parse(new Cursol("_public_"));

        result.IsSuccess.Should().BeTrue();
        result.Cursol.Index.Should().Be(8);
    }

    [Fact]
    public void 空白パーサーが消費して失敗しても前後とも失敗前の位置を使います()
    {
        var 呼び出し位置 = new List<int>();
        var tested = new WordParser("あ", new CallbackCharParser((current, index) =>
        {
            呼び出し位置.Add(index);
            return (current == '_', current, 1);
        }));

        var result = tested.Parse(new Cursol("_あ_!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("あ");
        result.Cursol.Index.Should().Be(3);
        result.Cursol.Current.Should().Be('!');
        呼び出し位置.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void 大文字小文字を無視して指定した空白パーサーで単語前後を読み飛ばします()
    {
        var tested = new WordParser("public", true, new CharParser('_'));

        var result = tested.Parse(new Cursol("_PUBLIC_"));

        result.IsSuccess.Should().BeTrue();
        result.Cursol.Index.Should().Be(8);
    }

    [Fact]
    public void 空白パーサーにnullは指定できません()
    {
        var action = () => new WordParser("public", false, null!);

        action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("whiteSpace");
    }

    [Fact]
    public void 同じパーサーで失敗後にも別の入力を読み込めます()
    {
        var tested = new WordParser("public", true);

        var failure = tested.Parse(new Cursol("publiX,"));
        var success = tested.Parse(new Cursol("PUBLIC,"));

        failure.IsSuccess.Should().BeFalse();
        failure.Cursol.Index.Should().Be(0);
        success.IsSuccess.Should().BeTrue();
        success.Parsed.Should().Be("PUBLIC");
        success.Cursol.Index.Should().Be(6);
    }

    [Fact]
    public void 空の単語でも指定した空白パーサーで読み飛ばします()
    {
        var tested = new WordParser("", new CharParser('_'));

        var result = tested.Parse(new Cursol("__x"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().BeEmpty();
        result.Cursol.Index.Should().Be(2);
    }

    [Fact]
    public void 大文字小文字を無視しない場合には異なる文字を読み込みません()
    {
        var tested = new WordParser("Public");

        tested.Parse(new Cursol("public,")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void 先頭の空白を読み飛ばした後に失敗した場合も元のカーソルを返します()
    {
        var cursol = new Cursol("x__publiX").GoFoward(1);
        var tested = new WordParser("public", new CharParser('_'));

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
        (result.Cursol == cursol).Should().BeTrue();
    }

    [Theory]
    [InlineData("a\0\0", "a")]
    [InlineData("\0", "")]
    [InlineData("a\0", "a\0")]
    public void 入力終端のNULも単語の文字として読み込めます(string word, string input)
    {
        var tested = new WordParser(word);

        var result = tested.Parse(new Cursol(input));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(word);
        result.Cursol.Index.Should().Be(input.Length);
    }

    [Fact]
    public void 負の位置では終端文字を読み込んでから文書の先頭へ進みます()
    {
        var tested = new WordParser("\0\0ab");

        var result = tested.Parse(new Cursol("ab,").GoFoward(-2));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("\0\0ab");
        result.Cursol.Index.Should().Be(2);
    }

    [Fact]
    public void 大文字小文字の比較には解析時のカルチャーを使用します()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var tested = new WordParser("I", true);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            var turkish = tested.Parse(new Cursol("ı,"));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var english = tested.Parse(new Cursol("ı,"));

            turkish.IsSuccess.Should().BeTrue();
            turkish.Parsed.Should().Be("ı");
            turkish.Cursol.Index.Should().Be(1);
            english.IsSuccess.Should().BeFalse();
            english.Cursol.Index.Should().Be(0);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IgnoreCaseはコンストラクターで指定した値を返します(bool ignoreCase)
    {
        var tested = new WordParser("public", ignoreCase, new CharParser('_'));

        tested.IgnoreCase.Should().Be(ignoreCase);
    }

    [Fact]
    public void IgnoreCaseは空白パーサーを省略しても大文字小文字を無視する指定を返します()
    {
        var tested = new WordParser("public", true);

        tested.IgnoreCase.Should().BeTrue();
    }

    [Fact]
    public void IgnoreCaseは大文字小文字の指定を省略するとfalseを返します()
    {
        new WordParser("public").IgnoreCase.Should().BeFalse();
        new WordParser("public", new CharParser('_')).IgnoreCase.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 単語がnullの場合は空白パーサーより先にsource引数の例外を返します(bool nullWhiteSpace)
    {
        var action = () => new WordParser(null!, true, nullWhiteSpace ? null! : new CharParser('_'));

        action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");
    }

    [Fact]
    public void 文字列入力を同じ単語パーサーで空白を読み飛ばして読み込めます()
    {
        Parser<string> tested = new WordParser("éあb");

        var result = tested.Parse(new Cursol(" éあb ,"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("éあb");
        result.Cursol.Index.Should().Be(5);
    }

}
