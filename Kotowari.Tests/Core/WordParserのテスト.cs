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

[Collection("WordParserの可変状態")]
public class WordParserのテスト
{
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

    [Fact]
    public void ParseAsyncは単語前のスペースを読み飛ばします()
    {
        var cursol = new Cursol(" public");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(7);
    }

    [Fact]
    public void ParseAsyncは単語後のスペースを読み飛ばします()
    {
        var cursol = new Cursol("public ");
        var tested = new WordParser("public");

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(7);
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
    public void 同じカーソルの文書が変わった場合にも現在の文字を読み込みます()
    {
        var tested = new WordParser("public", true);
        var cursol = new Cursol("public,");

        var first = tested.Parse(cursol);
        cursol.Text[0] = 'P';
        var second = tested.Parse(cursol);

        first.Parsed.Should().Be("public");
        second.Parsed.Should().Be("Public");
        second.Cursol.Index.Should().Be(6);
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
    public void 入力終端では変更された終端文字を読み込みます()
    {
        var original = Cursol.Null;
        try
        {
            Cursol.Null = 'X';
            var tested = new WordParser("axx", true);

            var result = tested.Parse(new Cursol("a"));

            result.IsSuccess.Should().BeTrue();
            result.Parsed.Should().Be("aXX");
            result.Cursol.Index.Should().Be(1);
        }
        finally
        {
            Cursol.Null = original;
        }
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

    [Fact]
    public void IgnoreCaseは大文字小文字を無視する指定でもfalseを返す既存の値を維持します()
    {
        var tested = new WordParser("public", true);

        tested.IgnoreCase.Should().BeFalse();
    }

    [Fact]
    public void 末尾の空白パーサーが文書を書き換えても読み込んだ値を保持します()
    {
        var cursol = new Cursol("Public,");
        var calls = 0;
        var tested = new WordParser("public", true, new ConditionalCharParser(_ =>
        {
            calls++;
            if (calls == 2)
            {
                cursol.Text[0] = 'X';
            }

            return false;
        }));

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be("Public");
        result.Cursol.Index.Should().Be(6);
        cursol.Text[0].Should().Be('X');
        calls.Should().Be(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 単語がnullの場合は空白パーサーより先にsource引数の例外を返します(bool nullWhiteSpace)
    {
        var action = () => new WordParser(null!, true, nullWhiteSpace ? null! : new CharParser('_'));

        action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");
    }
}

// Cursol.Nullを変更するテストが他のパーサーのテストへ影響しないよう分離する。
[CollectionDefinition("WordParserの可変状態", DisableParallelization = true)]
public class WordParserの可変状態のコレクション
{
}
