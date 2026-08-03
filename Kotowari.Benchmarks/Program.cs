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
            JsonParserBenchmarks.ValidateAll();
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
