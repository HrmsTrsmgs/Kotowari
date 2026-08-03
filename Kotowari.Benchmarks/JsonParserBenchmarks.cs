using BenchmarkDotNet.Attributes;
using System;
using System.IO;
using System.Text.Json;
using KotowariJson = Marimo.Parser.JSON;

namespace Marimo.Kotowari.Benchmarks;

[MemoryDiagnoser]
public class JsonParserBenchmarks
{
    private string _json = null!;

    [GlobalSetup]
    public void Setup()
    {
        _json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "test.json"));
    }

    [Benchmark]
    public object Kotowari()
    {
        return KotowariJson.Parse(_json);
    }

    [Benchmark(Baseline = true)]
    public JsonElement SystemTextJson()
    {
        using var document = JsonDocument.Parse(_json);
        return document.RootElement.Clone();
    }
}
