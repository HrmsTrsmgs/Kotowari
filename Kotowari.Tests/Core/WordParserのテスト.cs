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
    [Theory]
    [InlineData("éあb", "éあb!", false, "éあb", 6)]
    [InlineData("true", "TrUe!", true, "TrUe", 4)]
    [InlineData("éa", "ÉA!", true, "ÉA", 3)]
    [InlineData("", "!", false, "", 0)]
    [InlineData("a\0\0", "a", false, "a\0\0", 1)]
    [InlineData("\0", "", false, "\0", 0)]
    [InlineData("a\0", "a\0", false, "a\0", 2)]
    public void UTF8単語照合は入力の表記と終端位置を保ちます(
        string word, string input, bool ignoreCase, string parsed, int nextIndex)
    {
        var tested = new WordParser(word, ignoreCase);

        var result = tested.Parse(Cursol.FromUtf8(Encoding.UTF8.GetBytes(input)));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(parsed);
        result.Cursol.Index.Should().Be(nextIndex);
    }

    [Fact]
    public void UTF8単語照合は単独サロゲートの定義を置換文字に一致させません()
    {
        var tested = new WordParser("\ud800");

        tested.Parse(Cursol.FromUtf8("\ufffd"u8)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UTF8単語照合の途中でも補助平面文字の読み取りを拒否します()
    {
        var tested = new WordParser("ab");
        var parse = () => tested.Parse(Cursol.FromUtf8("a😀"u8)).IsSuccess;

        parse.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UTF8単語照合の大小文字比較は解析時のカルチャーを使います()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var tested = new WordParser("I", true);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            var turkish = tested.Parse(Cursol.FromUtf8("ı!"u8));
            turkish.IsSuccess.Should().BeTrue();
            turkish.Parsed.Should().Be("ı");
            turkish.Cursol.Index.Should().Be(2);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            tested.Parse(Cursol.FromUtf8("ı!"u8)).IsSuccess.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void ParseAsyncは指定した単語を読み込みに成功します()
    {
        var cursol = Cursol.FromString("public");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncは指定していない単語を読み込みに失敗します()
    {
        var cursol = Cursol.FromString("publi");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }
    [Fact]
    public void ParseAsyncは指定した単語を読み込みます()
    {
        var cursol = Cursol.FromString("public");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.Parsed.Should().Be("public");
    }


    [Fact]
    public void ParseAsyncは読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
    {
        var cursol = Cursol.FromString("void");
        var tested = new WordParser("void");

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(4);
    }

    [Fact]
    public void ParseAsyncは読み込みに失敗した場合には進んでいないカーソルを返します()
    {
        var cursol = Cursol.FromString("publi");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(0);
    }

    [Fact]
    public void ParseAsyncは単語前のスペースを読み飛ばします()
    {
        var cursol = Cursol.FromString(" public");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(7);
    }

    [Fact]
    public void ParseAsyncは単語後のスペースを読み飛ばします()
    {
        var cursol = Cursol.FromString("public ");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(7);
    }

    [Fact]
    public void 指定した空白パーサーで単語前後を読み飛ばします()
    {
        var tested = new WordParser("public", new CharParser('_'));

        var result = tested.Parse(Cursol.FromString("_public_"));

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

        var utf16 = tested.Parse(Cursol.FromString("_あ_!"));

        utf16.IsSuccess.Should().BeTrue();
        utf16.Parsed.Should().Be("あ");
        utf16.Cursol.Index.Should().Be(3);
        utf16.Cursol.Current.Should().Be('!');
        呼び出し位置.Should().Equal(0, 1, 2, 3);
        呼び出し位置.Clear();

        var utf8 = tested.Parse(Cursol.FromUtf8("_あ_!"u8));

        utf8.IsSuccess.Should().BeTrue();
        utf8.Parsed.Should().Be("あ");
        utf8.Cursol.Index.Should().Be(5);
        utf8.Cursol.Current.Should().Be('!');
        呼び出し位置.Should().Equal(0, 1, 4, 5);
    }

    [Fact]
    public void 大文字小文字を無視して指定した空白パーサーで単語前後を読み飛ばします()
    {
        var tested = new WordParser("public", true, new CharParser('_'));

        var result = tested.Parse(Cursol.FromString("_PUBLIC_"));

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

        var failure = tested.Parse(Cursol.FromString("publiX,"));
        var success = tested.Parse(Cursol.FromString("PUBLIC,"));

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

        var result = tested.Parse(Cursol.FromString("__x"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().BeEmpty();
        result.Cursol.Index.Should().Be(2);
    }

    [Fact]
    public void 大文字小文字を無視しない場合には異なる文字を読み込みません()
    {
        var tested = new WordParser("Public");

        tested.Parse(Cursol.FromString("public,")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void 先頭の空白を読み飛ばした後に失敗した場合も元のカーソルを返します()
    {
        var cursol = Cursol.FromString("x__publiX").GoFoward(1);
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

        var result = tested.Parse(Cursol.FromString(input));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(word);
        result.Cursol.Index.Should().Be(input.Length);
    }

    [Fact]
    public void 負の位置では終端文字を読み込んでから文書の先頭へ進みます()
    {
        var tested = new WordParser("\0\0ab");

        var result = tested.Parse(Cursol.FromString("ab,").GoFoward(-2));

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

            var turkish = tested.Parse(Cursol.FromString("ı,"));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var english = tested.Parse(Cursol.FromString("ı,"));

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
    public void UTF8入力も同じ単語パーサーで空白を読み飛ばして読み込めます()
    {
        Parser<string> tested = new WordParser("éあb");

        var utf16 = tested.Parse(Cursol.FromString(" éあb ,"));
        var utf8 = tested.Parse(Cursol.FromUtf8(Encoding.UTF8.GetBytes(" éあb ,")));

        utf16.IsSuccess.Should().BeTrue();
        utf16.Parsed.Should().Be("éあb");
        utf16.Cursol.Index.Should().Be(5);
        utf8.IsSuccess.Should().BeTrue();
        utf8.Parsed.Should().Be("éあb");
        utf8.Cursol.Index.Should().Be(8);
        utf8.Cursol.Current.Should().Be(',');
    }

    [Fact]
    public void UTF8入力で単語が一致しなければ元のカーソルを返します()
    {
        var cursol = Cursol.FromUtf8(Encoding.UTF8.GetBytes("é あc,")).GoFoward(1);
        var tested = new WordParser("あb");

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
        (result.Cursol == cursol).Should().BeTrue();
        result.Cursol.Index.Should().Be(2);
        result.Cursol.Current.Should().Be(' ');
    }
}
