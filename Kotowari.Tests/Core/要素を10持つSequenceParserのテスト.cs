using FluentAssertions;
using Marimo.Kotowari.Core;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class 要素を10持つSequenceParserのテスト
{
    const string SuccessInput = "ab cd ef gh ij kl mn op qr st";

    static Parser<(string, string, string, string, string, string, string, string, string, string)> Tested
        => new SequenceParser<string, string, string, string, string, string, string, string, string, string>(
            new WordParser("ab"),
            new WordParser("cd"),
            new WordParser("ef"),
            new WordParser("gh"),
            new WordParser("ij"),
            new WordParser("kl"),
            new WordParser("mn"),
            new WordParser("op"),
            new WordParser("qr"),
            new WordParser("st"))
            .WithWhiteSpace(new CharParser(' '));
    [Fact]
    public void 親に指定した空白規則で十個の単語を読み込み末尾まで進みます()
    {
        var result = Tested.Parse(new Cursol(SuccessInput));

        result.IsSuccess.Should().BeTrue();
        result.Parsed.Should().Be(("ab", "cd", "ef", "gh", "ij", "kl", "mn", "op", "qr", "st"));
        result.Cursol.Index.Should().Be(SuccessInput.Length);
    }

    public static TheoryData<string> FailedInputs => SequenceTestData.FailedInputs(SuccessInput);

    [Theory]
    [MemberData(nameof(FailedInputs))]
    public void 親に空白規則を指定してもいずれかの単語が一致しなければ失敗します(string 入力)
    {
        Tested.Parse(new Cursol(入力)).IsSuccess.Should().BeFalse();
    }
}
