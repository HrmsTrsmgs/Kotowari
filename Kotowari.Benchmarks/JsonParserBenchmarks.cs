using BenchmarkDotNet.Attributes;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text.Json;
using KotowariJson = Marimo.Parser.Json;

namespace Marimo.Kotowari.Benchmarks;

[MemoryDiagnoser]
public class JsonParserBenchmarks
{
    private string _json = null!;

    [GlobalSetup]
    public void Setup()
        => _json = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Data", "test.json"));

    public static bool ValidateAll()
    {
        var hasFailures = false;
        var benchmarks = new JsonParserBenchmarks();
        benchmarks.Setup();
        JsonElement systemTextJsonResult = default;
        Exception? systemTextJsonException = null;
        try
        {
            systemTextJsonResult = benchmarks.SystemTextJson();
        }
        catch (Exception exception)
        {
            systemTextJsonException = exception;
        }

        foreach (var (name, parse) in new (string, Func<object>)[]
        {
            (nameof(Kotowari), benchmarks.Kotowari),
            (nameof(Pidgin), benchmarks.Pidgin),
            (nameof(Sprache), benchmarks.Sprache),
            (nameof(SpracheJsonPackage), benchmarks.SpracheJsonPackage),
            (nameof(Superpower), benchmarks.Superpower),
            (nameof(SystemTextJson), () => systemTextJsonResult)
        })
        {
            try
            {
                if (name == nameof(SystemTextJson) && systemTextJsonException is not null)
                {
                    throw systemTextJsonException;
                }

                var result = parse();
                if (name == nameof(Kotowari))
                {
                    var validation = KotowariJson.ParseForValidation(benchmarks._json);
                    if (!validation.IsSuccess || validation.Cursol.Index != benchmarks._json.Length)
                    {
                        throw new InvalidOperationException("Parser did not consume the entire input.");
                    }
                }
                else if (name == nameof(SpracheJsonPackage))
                {
                    var parserField = typeof(SpracheJSON.JSON).Assembly.GetType("SpracheJSON.JSONParser")?
                        .GetField("JObject", BindingFlags.Static | BindingFlags.NonPublic)
                        ?? throw new InvalidOperationException("SpracheJSON root parser field was not found.");
                    if (parserField.FieldType != typeof(Sprache.Parser<SpracheJSON.IJSONValue>))
                    {
                        throw new InvalidOperationException("SpracheJSON root parser field has an unexpected type.");
                    }

                    var parser = (Sprache.Parser<SpracheJSON.IJSONValue>)parserField.GetValue(null)!;
                    var validation = parser(new Sprache.Input(benchmarks._json));
                    if (!validation.WasSuccessful || !validation.Remainder.AtEnd)
                    {
                        throw new InvalidOperationException("Parser did not consume the entire input.");
                    }
                }

                var matchesSystemTextJson = name is nameof(Pidgin) or nameof(Sprache) or nameof(Superpower);
                if (matchesSystemTextJson && systemTextJsonException is not null)
                {
                    throw new InvalidOperationException("System.Text.Json reference parsing failed.", systemTextJsonException);
                }

                if (matchesSystemTextJson && !MatchesSystemTextJson((BenchmarkJson)result, systemTextJsonResult))
                {
                    throw new InvalidOperationException("Result does not match System.Text.Json.");
                }

                var matchDescription = matchesSystemTextJson ? "; matches SystemTextJson" : string.Empty;
                Console.WriteLine($"{name}: OK ({CountNodes(result):N0} nodes{matchDescription})");
            }
            catch (Exception exception)
            {
                hasFailures = true;
                Console.WriteLine($"{name}: FAILED - {exception.Message}");
            }
        }

        return !hasFailures;
    }

    private static int CountNodes(object result) => result switch
    {
        BenchmarkJson value => CountNodes(value),
        Marimo.Parser.JsonObject value => 1 + value.Pairs.Values.Sum(CountNodes),
        Marimo.Parser.JsonArray value => 1 + value.Elements.Sum(CountNodes),
        Marimo.Parser.JsonLiteral => 1,
        SpracheJSON.JSONObject value => 1 + value.Pairs.Values.Sum(CountNodes),
        SpracheJSON.JSONArray value => 1 + value.Elements.Sum(CountNodes),
        SpracheJSON.JSONLiteral => 1,
        JsonElement value => CountNodes(value),
        _ => throw new InvalidOperationException($"Unknown JSON result type: {result.GetType()}")
    };

    private static int CountNodes(BenchmarkJson result) => result switch
    {
        BenchmarkJsonObject value => 1 + value.Members.Values.Sum(CountNodes),
        BenchmarkJsonArray value => 1 + value.Elements.Sum(CountNodes),
        BenchmarkJsonBoolean => 1,
        BenchmarkJsonNull => 1,
        BenchmarkJsonNumber => 1,
        BenchmarkJsonString => 1
    };

    private static int CountNodes(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => 1 + element.EnumerateObject().Sum(property => CountNodes(property.Value)),
        JsonValueKind.Array => 1 + element.EnumerateArray().Sum(CountNodes),
        _ => 1
    };

    private static bool MatchesSystemTextJson(BenchmarkJson result, JsonElement expected) => result switch
    {
        BenchmarkJsonObject value => expected.ValueKind == JsonValueKind.Object
            && value.Members.Count == expected.EnumerateObject().Count()
            && value.Members.All(member => expected.TryGetProperty(member.Key, out var expectedValue)
                && MatchesSystemTextJson(member.Value, expectedValue)),
        BenchmarkJsonArray value => expected.ValueKind == JsonValueKind.Array
            && value.Elements.Count == expected.GetArrayLength()
            && value.Elements.Select((element, index) => MatchesSystemTextJson(element, expected[index])).All(match => match),
        BenchmarkJsonBoolean value => expected.ValueKind is JsonValueKind.True or JsonValueKind.False
            && value.Value == expected.GetBoolean(),
        BenchmarkJsonNull => expected.ValueKind == JsonValueKind.Null,
        BenchmarkJsonNumber value => expected.ValueKind == JsonValueKind.Number
            && string.Equals(value.Value, expected.GetRawText(), StringComparison.Ordinal),
        BenchmarkJsonString value => expected.ValueKind == JsonValueKind.String
            && string.Equals(value.Value, expected.GetString(), StringComparison.Ordinal)
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
