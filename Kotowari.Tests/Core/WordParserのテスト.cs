using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

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

        action.Should().Throw<ArgumentNullException>();
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
}
