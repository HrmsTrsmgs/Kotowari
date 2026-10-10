using System.Runtime.CompilerServices;

namespace Marimo.Kotowari;

// 文法定義の寿命に結び付け、入力型別の実行器を共有する。
// 候補の初回使用、既存ParseCoreからの入口、動的な変換定義の子の共有に使う。
// 固定した子の反復中は、構成済みの実行器を直接呼ぶ。
internal static class ParserExecutionCache<TDefinition, TExecution>
    where TDefinition : class
    where TExecution : class
{
    static readonly ConditionalWeakTable<TDefinition, TExecution> cache = [];

    internal static TExecution Get(
        TDefinition definition,
        ConditionalWeakTable<TDefinition, TExecution>.CreateValueCallback create)
        => cache.GetValue(definition, create);
}
