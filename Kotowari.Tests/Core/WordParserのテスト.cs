using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Globalization;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class WordParserのテスト
{
    [Fact]
    public void 指定した単語の読み込みに成功します()
    {
        var cursol = new Cursol("public");
        var tested = new WordParser("public");

        tested.Parse(cursol).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void 指定していない単語の読み込みに失敗します()
    {
        var cursol = new Cursol("publi");
        var tested = new WordParser("public");

        tested.Parse(cursol).IsSuccess.Should().BeFalse();
    }
    [Fact]
    public void 指定した単語を読み込みます()
    {
        var cursol = new Cursol("public");
        var tested = new WordParser("public");

        tested.Parse(cursol).Parsed.Should().Be("public");
    }

    [Fact]
    public void 読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
    {
        var cursol = new Cursol("void");
        var tested = new WordParser("void");

        tested.Parse(cursol).Cursol.Index.Should().Be(4);
    }

    [Fact]
    public void 読み込みに失敗した場合には進んでいないカーソルを返します()
    {
        var cursol = new Cursol("publi");
        var tested = new WordParser("public");

        tested.Parse(cursol).Cursol.Index.Should().Be(0);
    }

    [Fact]
    public void 指定文字列の前に空白があると一致せず入力を消費しません()
    {
        var cursol = new Cursol(" public");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
        result.Cursol.Should().Be(cursol);
    }

    [Fact]
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

    [Fact]
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
    public void 指定文字列が空でも空白は消費しません()
    {
        var cursol = new Cursol(" x");
        var tested = new WordParser("");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().BeEmpty();
        result.Cursol.Should().Be(cursol);
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
    public void 大文字小文字を無視しない場合には異なる文字を読み込みません()
    {
        var tested = new WordParser("Public");

        tested.Parse(new Cursol("public,")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void 開始位置が0でなくても単語の照合に失敗したら元のカーソルを返します()
    {
        var cursol = new Cursol("xpubliX").GoFoward(1);
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
        result.Cursol.Should().Be(cursol);
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
        var tested = new WordParser("public", ignoreCase);

        tested.IgnoreCase.Should().Be(ignoreCase);
    }

    [Fact]
    public void IgnoreCaseは大文字小文字を無視する指定を返します()
    {
        var tested = new WordParser("public", true);

        tested.IgnoreCase.Should().BeTrue();
    }

    [Fact]
    public void IgnoreCaseは大文字小文字の指定を省略するとfalseを返します()
    {
        new WordParser("public").IgnoreCase.Should().BeFalse();
    }

    [Fact]
    public void 文字列入力を同じ単語パーサーで読み込めます()
    {
        Parser<string> tested = new WordParser("éあb");

        var result = tested.Parse(new Cursol("éあb,"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("éあb");
        result.Cursol.Index.Should().Be(3);
    }

}
