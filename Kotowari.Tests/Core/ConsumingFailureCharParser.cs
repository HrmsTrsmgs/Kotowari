using Marimo.Kotowari.Core;

namespace Marimo.Kotowari.Tests.Core;

// 外部派生型のTryParseCoreを通ることを観測する。
internal sealed class ConsumingFailureCharParser : Parser<char>
{
    protected override bool TryParseCore(Cursol start, out Cursol next, out char parsed)
    {
        next = start.GoFoward(2);
        parsed = default;
        return false;
    }
}
