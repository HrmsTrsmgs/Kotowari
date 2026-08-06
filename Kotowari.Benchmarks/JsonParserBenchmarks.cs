using BenchmarkDotNet.Attributes;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using KotowariJson = Marimo.Parser.JSON;

namespace Marimo.Kotowari.Benchmarks;

[MemoryDiagnoser]
public class JsonParserBenchmarks
{
    private string _json = null!;

    [GlobalSetup]
    public void Setup()
        => _json = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Data", "test.json"));

    public static void ValidateAll()
    {
        var benchmarks = new JsonParserBenchmarks();
        benchmarks.Setup();
        foreach (var (name, parse) in new (string, Func<object>)[]
        {
            (nameof(Kotowari), benchmarks.Kotowari),
            (nameof(Pidgin), benchmarks.Pidgin),
            (nameof(Sprache), benchmarks.Sprache),
            (nameof(SpracheJsonPackage), benchmarks.SpracheJsonPackage),
            (nameof(Superpower), benchmarks.Superpower),
            (nameof(SystemTextJson), () => benchmarks.SystemTextJson())
        })
        {
            try
            {
                var result = parse();
                Console.WriteLine($"{name}: OK ({CountNodes(result):N0} nodes)");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{name}: FAILED - {exception.Message}");
            }
        }
    }

    private static int CountNodes(object result) => result switch
    {
        BenchmarkJsonObject value => 1 + value.Members.Values.Sum(CountNodes),
        BenchmarkJsonArray value => 1 + value.Elements.Sum(CountNodes),
        BenchmarkJson => 1,
        Marimo.Parser.JSONObject value => 1 + value.Pairs.Values.Sum(CountNodes),
        Marimo.Parser.JSONArray value => 1 + value.Elements.Sum(CountNodes),
        Marimo.Parser.JSONLiteral => 1,
        SpracheJSON.JSONObject value => 1 + value.Pairs.Values.Sum(CountNodes),
        SpracheJSON.JSONArray value => 1 + value.Elements.Sum(CountNodes),
        SpracheJSON.JSONLiteral => 1,
        JsonElement value => CountNodes(value),
        _ => throw new InvalidOperationException($"Unknown JSON result type: {result.GetType()}")
    };

    private static int CountNodes(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => 1 + element.EnumerateObject().Sum(property => CountNodes(property.Value)),
        JsonValueKind.Array => 1 + element.EnumerateArray().Sum(CountNodes),
        _ => 1
    };

    [Benchmark(Baseline = true)]
    public object Kotowari() => KotowariJson.Parse(_json);

    [Benchmark]
    public object Pidgin() => PidginJsonParser.Parse(_json);

    [Benchmark]
    public object Sprache() => SpracheJsonParser.Parse(_json);

    [Benchmark]
    public object SpracheJsonPackage() => SpracheJSON.JSON.Parse(_json);

    [Benchmark]
    public object Superpower() => SuperpowerJsonParser.Parse(_json);

    [Benchmark]
    public JsonElement SystemTextJson()
    {
        using var document = JsonDocument.Parse(_json);
        return document.RootElement.Clone();
    }
}
