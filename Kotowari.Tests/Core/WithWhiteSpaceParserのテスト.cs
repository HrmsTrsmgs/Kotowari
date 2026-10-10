using FluentAssertions;
using Marimo.Kotowari.Core;

using System.Collections.Generic;

using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class WithWhiteSpaceParserのテスト
{
    [Fact]
    public void 空白パーサーが消費して失敗しても失敗前の位置から子を読み後の空白は残します()
    {
        var 呼び出し位置 = new List<int>();
        var tested = new WithWhiteSpaceParser<char>(new CallbackCharParser((current, index) =>
        {
            呼び出し位置.Add(index);
            return (current == '_', current, 1);
        }), new CharParser('あ'));

        var utf16 = tested.Parse(Cursol.FromString("_あ_!"));

        utf16.IsSuccess.Should().BeTrue();
        utf16.Parsed.Should().Be('あ');
        utf16.Cursol.Index.Should().Be(2);
        utf16.Cursol.Current.Should().Be('_');
        呼び出し位置.Should().Equal(0, 1);
        呼び出し位置.Clear();

        var utf8 = tested.Parse(Cursol.FromUtf8("_あ_!"u8));

        utf8.IsSuccess.Should().BeTrue();
        utf8.Parsed.Should().Be('あ');
        utf8.Cursol.Index.Should().Be(4);
        utf8.Cursol.Current.Should().Be('_');
        呼び出し位置.Should().Equal(0, 1);
    }

    [Fact]
    public void 前の空白を読み飛ばした後は子の失敗時の消費位置を返します()
    {
        var tested = new WithWhiteSpaceParser<char>(
            new CharParser('_'),
            new CallbackCharParser((_, _) => (false, '?', 1)));

        var utf16 = tested.Parse(Cursol.FromString("_あ!"));
        var utf8 = tested.Parse(Cursol.FromUtf8("_あ!"u8));

        utf16.IsSuccess.Should().BeFalse();
        utf16.Cursol.Index.Should().Be(2);
        utf16.Cursol.Current.Should().Be('!');
        utf8.IsSuccess.Should().BeFalse();
        utf8.Cursol.Index.Should().Be(4);
        utf8.Cursol.Current.Should().Be('!');
    }
}
