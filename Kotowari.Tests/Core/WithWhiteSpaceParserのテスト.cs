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

        var result = tested.Parse(new Cursol("_あ_!"));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be('あ');
        result.Cursol.Index.Should().Be(2);
        result.Cursol.Current.Should().Be('_');
        呼び出し位置.Should().Equal(0, 1);
    }

    [Fact]
    public void 前の空白を読み飛ばした後は子の失敗時の消費位置を返します()
    {
        var tested = new WithWhiteSpaceParser<char>(
            new CharParser('_'),
            new CallbackCharParser((_, _) => (false, '?', 1)));

        var result = tested.Parse(new Cursol("_あ!"));

        result.IsSuccess.Should().BeFalse();
        result.Cursol.Index.Should().Be(2);
        result.Cursol.Current.Should().Be('!');
    }
}
