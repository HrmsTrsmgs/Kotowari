using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class CharParserのテスト
{
    [Fact]
    public void ParseAsyncは指定した文字を読み込みに成功します()
    {
        var cursol = Cursol.FromString("public");
        var tested = new CharParser('p');

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncは指定した文字を読み込みます()
    {
        var cursol = Cursol.FromString("public");
        var tested = new CharParser('p');

        var result = tested.Parse(cursol);

        result.Parsed.Should().Be('p');
    }

    [Fact]
    public void ParseAsyncは指定していない文字を読み込みに失敗します()
    {
        var cursol = Cursol.FromString("internal");
        var tested = new CharParser('p');

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncは読み込みに成功した場合にその分進んだカーソルを返します()
    {
        var cursol = Cursol.FromString("public");
        var tested = new CharParser('p');

        var result = tested.Parse(cursol);

        result.Cursol.Index.Should().Be(1);
    }

    [Fact]
    public void ParseAsyncはIgnoreCaseでない場合に小文字指定の場合に大文字を区別します()
    {
        var cursol = Cursol.FromString("Public");
        var tested = new CharParser('p');

        var isSuccess = tested.Parse(cursol).IsSuccess;

        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncはIgnoreCaseでない場合に大文字指定の場合に小文字を区別します()
    {
        var cursol = Cursol.FromString("public");
        var tested = new CharParser('P');

        var isSuccess = tested.Parse(cursol).IsSuccess;

        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseAsyncはIgnoreCaseで小文字指定でも大文字を識別します()
    {
        var cursol = Cursol.FromString("Public");
        var tested = new CharParser('p', true);

        var isSuccess = tested.Parse(cursol).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncはIgnoreCaseで大文字指定でも小文字を識別します()
    {
        var cursol = Cursol.FromString("public");
        var tested = new CharParser('P', true);

        var isSuccess = tested.Parse(cursol).IsSuccess;

        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseAsyncはIgnoreCaseで小文字指定の場合に実際に識別した文字を結果とします()
    {
        var cursol = Cursol.FromString("Public");
        var tested = new CharParser('p', true);

        var parsed = tested.Parse(cursol).Parsed;

        parsed.Should().Be('P');
    }

    [Fact]
    public void ParseAsyncはIgnoreCaseで大文字指定の場合に実際に識別した文字を結果とします()
    {
        var cursol = Cursol.FromString("public");
        var tested = new CharParser('P', true);

        var parsed = tested.Parse(cursol).Parsed;

        parsed.Should().Be('p');
    }

    [Theory]
    [InlineData("ab", 'a', 1)]
    [InlineData("éb", 'é', 2)]
    [InlineData("あb", 'あ', 3)]
    public void UTF8入力も同じ文字パーサーで読み込めます(string input, char first, int nextIndex)
    {
        Parser<char> tested = new CharParser(first);

        var utf16 = tested.Parse(Cursol.FromString(input));
        var utf8 = tested.Parse(Cursol.FromUtf8(Encoding.UTF8.GetBytes(input)));

        utf16.IsSuccess.Should().BeTrue();
        utf16.Parsed.Should().Be(first);
        utf16.Cursol.Index.Should().Be(1);
        utf8.IsSuccess.Should().BeTrue();
        utf8.Parsed.Should().Be(first);
        utf8.Cursol.Index.Should().Be(nextIndex);
        utf8.Cursol.Current.Should().Be('b');
    }

    [Fact]
    public void UTF8入力で文字が一致しなければ元のカーソルを返します()
    {
        var cursol = Cursol.FromUtf8(Encoding.UTF8.GetBytes("あb"));
        var tested = new CharParser('い');

        var result = tested.Parse(cursol);

        result.IsSuccess.Should().BeFalse();
        (result.Cursol == cursol).Should().BeTrue();
        result.Cursol.Index.Should().Be(0);
        result.Cursol.Current.Should().Be('あ');
    }
}
