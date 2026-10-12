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

public class CharParserのテスト
{
    [Theory]
    [InlineData("ab", 'a', false, 'a', 1)]
    [InlineData("éb", 'é', false, 'é', 2)]
    [InlineData("あb", 'あ', false, 'あ', 3)]
    [InlineData("Ab", 'a', true, 'A', 1)]
    [InlineData("Éb", 'é', true, 'É', 2)]
    [InlineData("\0b", '\0', false, '\0', 1)]
    [InlineData("", '\0', false, '\0', 0)]
    public void UTF8照合は入力の文字と消費バイト数を返します(
        string input, char expected, bool ignoreCase, char parsed, int nextIndex)
    {
        var tested = new CharParser(expected, ignoreCase);

        var result = tested.Parse(Cursol.FromUtf8(Encoding.UTF8.GetBytes(input)));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(parsed);
        result.Cursol.Index.Should().Be(nextIndex);
    }

    [Fact]
    public void UTF8入力の置換文字は定義した単独サロゲートに一致しません()
    {
        var tested = new CharParser('\ud800');

        tested.Parse(Cursol.FromUtf8("\ufffd"u8)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UTF8文字照合でも補助平面文字の読み取りを拒否します()
    {
        var tested = new CharParser('a');
        var parse = () => tested.Parse(Cursol.FromUtf8("😀"u8)).IsSuccess;

        parse.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UTF8文字照合の大小文字比較は解析時のカルチャーを使います()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var tested = new CharParser('I', true);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            var turkish = tested.Parse(Cursol.FromUtf8("ı!"u8));
            turkish.IsSuccess.Should().BeTrue();
            turkish.Parsed.Should().Be('ı');
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

    [Fact]
    public void 組み合わせから呼んでも派生文字パーサーの処理と基底実装を使います()
    {
        var parser = new UpperCaseCharParser();
        var tested = new ZeroOrMoreParser<char>(
            new OrParser<char>(parser, new CharParser('b')));

        var utf16 = tested.Parse(Cursol.FromString("ab!"));
        var utf8 = tested.Parse(Cursol.FromUtf8("ab!"u8));

        utf16.Parsed.Should().Equal('A', 'b');
        utf16.Cursol.Index.Should().Be(2);
        utf8.Parsed.Should().Equal('A', 'b');
        utf8.Cursol.Index.Should().Be(2);
        parser.呼び出し回数.Should().Be(6);
    }
}
