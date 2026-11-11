using FluentAssertions;
using Marimo.Kotowari.Core;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class Optionalのテスト
{
    [Fact]
    public void 存在する値を保持します()
    {
        var tested = new Optional<string>(true, "value");

        tested.IsPresent.Should().BeTrue();
        tested.Value.Should().Be("value");
    }

    [Fact]
    public void 不在の場合は指定された値を保持しません()
    {
        var tested = new Optional<string>(false, "value");

        tested.IsPresent.Should().BeFalse();
        tested.Value.Should().BeNull();
    }

    [Fact]
    public void 存在するnullは不在と区別されます()
    {
        var present = new Optional<string?>(true, null);
        var absent = new Optional<string?>(false, null);

        present.IsPresent.Should().BeTrue();
        present.Value.Should().BeNull();
        absent.IsPresent.Should().BeFalse();
        (present == absent).Should().BeFalse();
    }

    [Fact]
    public void 存在するnull許容値型のnullも保持します()
    {
        var present = new Optional<int?>(true, null);

        present.IsPresent.Should().BeTrue();
        present.Value.Should().BeNull();
    }

    [Fact]
    public void 不在のOptionalは指定された値にかかわらず等価です()
    {
        var first = new Optional<string>(false, "first");
        var second = new Optional<string>(false, "second");

        (first == second).Should().BeTrue();
    }
}
