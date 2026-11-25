using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace Marimo.Kotowari.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--validate", StringComparer.Ordinal))
        {
            Environment.ExitCode = JsonParserBenchmarks.ValidateAll() ? 0 : 1;
            return;
        }

        var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
