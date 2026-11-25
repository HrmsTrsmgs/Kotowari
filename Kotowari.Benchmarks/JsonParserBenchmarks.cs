using BenchmarkDotNet.Attributes;
using System;
using System.IO;
using System.Text.Json;
using KotowariJson = Marimo.Parser.Json;

namespace Marimo.Kotowari.Benchmarks;

[MemoryDiagnoser]
public class JsonParserBenchmarks
{
    // Both BenchmarkDotNet and ValidateAll call Setup before invoking a parser.
    string json = null!;

    [GlobalSetup]
    public void Setup()
        => json = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Data", "test.json"));

    public static bool ValidateAll()
    {
        var benchmarks = new JsonParserBenchmarks();
        benchmarks.Setup();
        return JsonBenchmarkValidation.Run(benchmarks, benchmarks.json);
    }

    [Benchmark(Baseline = true)]
    public object Kotowari() => KotowariJson.Parse(json);

    [Benchmark]
    public object Pidgin() => PidginJsonParser.Parse(json);

    [Benchmark]
    public object Sprache() => SpracheJsonParser.Parse(json);

    [Benchmark]
    public object SpracheJsonPackage() => SpracheJSON.JSON.Parse(json);

    [Benchmark]
    public object Superpower() => SuperpowerJsonParser.Parse(json);

    [Benchmark]
    public JsonElement SystemTextJson()
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
