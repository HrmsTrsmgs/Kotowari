using System.Runtime.CompilerServices;

namespace Marimo.Kotowari.Core;

// 入力長に比例するstackallocは行わず、大きな値型にも使うため8要素に制限する。
[InlineArray(8)]
struct ParsedValueScratchBuffer<T>
{
    T first;
}
