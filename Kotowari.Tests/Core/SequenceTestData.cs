using Xunit;

namespace Marimo.Kotowari.Tests.Core;

static class SequenceTestData
{
    internal static TheoryData<string> FailedInputs(string successInput)
    {
        var words = successInput.Split(' ');
        var cases = new TheoryData<string>();
        for (var i = 0; i < words.Length; i++)
        {
            var original = words[i];
            words[i] = original[..^1] + (i == words.Length - 1 ? "" : "x");
            cases.Add(string.Join(' ', words));
            words[i] = original;
        }
        return cases;
    }
}
