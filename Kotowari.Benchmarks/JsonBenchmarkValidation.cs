using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using KotowariJson = Marimo.Parser.Json;

namespace Marimo.Kotowari.Benchmarks;

/// <summary>Checks each benchmark result outside the timed methods and reports failures independently.</summary>
internal static class JsonBenchmarkValidation
{
    internal static bool Run(JsonParserBenchmarks benchmarks, string input)
    {
        // Parse the reference once. Its failure must not prevent unrelated parsers from being checked.
        JsonElement reference = default;
        Exception? referenceFailure = null;
        try
        {
            reference = benchmarks.SystemTextJson();
        }
        catch (Exception exception)
        {
            referenceFailure = exception;
        }

        var allSucceeded = true;
        Report(nameof(benchmarks.Kotowari),
            () => CountFullyConsumed(benchmarks.Kotowari, () => KotowariConsumedInput(input)));
        CompareWithReference(nameof(benchmarks.Pidgin), benchmarks.Pidgin);
        CompareWithReference(nameof(benchmarks.Sprache), benchmarks.Sprache);
        Report(nameof(benchmarks.SpracheJsonPackage),
            () => CountFullyConsumed(benchmarks.SpracheJsonPackage, () => SprachePackageConsumedInput(input)));
        CompareWithReference(nameof(benchmarks.Superpower), benchmarks.Superpower);
        Report(nameof(benchmarks.SystemTextJson),
            () => referenceFailure is null ? CountNodes(reference) : throw referenceFailure);
        return allSucceeded;

        void CompareWithReference(string name, Func<object> parse) =>
            Report(name, () =>
            {
                var result = parse();
                if (referenceFailure is not null)
                {
                    throw new InvalidOperationException("System.Text.Json reference parsing failed.", referenceFailure);
                }

                var tree = (BenchmarkJson)result;
                if (!MatchesSystemTextJson(tree, reference))
                {
                    throw new InvalidOperationException("Result does not match System.Text.Json.");
                }

                return CountNodes(tree);
            }, "; matches SystemTextJson");

        void Report(string name, Func<int> validate, string matchDescription = "")
        {
            try
            {
                Console.WriteLine($"{name}: OK ({validate():N0} nodes{matchDescription})");
            }
            catch (Exception exception)
            {
                allSucceeded = false;
                Console.WriteLine($"{name}: FAILED - {exception.Message}");
            }
        }
    }

    // The timed entry points expose only the value. Check the root parser's cursor separately.
    static int CountFullyConsumed(Func<object> parse, Func<bool> consumedInput)
    {
        var value = parse();
        if (!consumedInput())
        {
            throw new InvalidOperationException("Parser did not consume the entire input.");
        }

        return CountNodes(value);
    }

    static bool KotowariConsumedInput(string input)
    {
        var result = KotowariJson.ParseDocument(input);
        return result.IsSuccess && result.Cursol.Index == input.Length;
    }

    static bool SprachePackageConsumedInput(string input)
    {
        // The pinned legacy package exposes its parser only through this initialized static field.
        var parserField = typeof(SpracheJSON.JSON).Assembly.GetType("SpracheJSON.JSONParser")?
            .GetField("JObject", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SpracheJSON root parser field was not found.");
        if (parserField.FieldType != typeof(Sprache.Parser<SpracheJSON.IJSONValue>))
        {
            throw new InvalidOperationException("SpracheJSON root parser field has an unexpected type.");
        }

        var parser = (Sprache.Parser<SpracheJSON.IJSONValue>)parserField.GetValue(null)!;
        var result = parser(new Sprache.Input(input));
        return result.WasSuccessful && result.Remainder.AtEnd;
    }

    static int CountNodes(object result) => result switch
    {
        Marimo.Parser.JsonObject value => 1 + value.Pairs.Values.Sum(CountNodes),
        Marimo.Parser.JsonArray value => 1 + value.Elements.Sum(CountNodes),
        Marimo.Parser.JsonLiteral => 1,
        SpracheJSON.JSONObject value => 1 + value.Pairs.Values.Sum(CountNodes),
        SpracheJSON.JSONArray value => 1 + value.Elements.Sum(CountNodes),
        SpracheJSON.JSONLiteral => 1,
        _ => throw new InvalidOperationException($"Unknown JSON result type: {result.GetType()}")
    };

    static int CountNodes(BenchmarkJson result) => result switch
    {
        BenchmarkJsonObject value => 1 + value.Members.Values.Sum(CountNodes),
        BenchmarkJsonArray value => 1 + value.Elements.Sum(CountNodes),
        BenchmarkJsonBoolean or BenchmarkJsonNull or BenchmarkJsonNumber or BenchmarkJsonString => 1
    };

    static int CountNodes(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => 1 + element.EnumerateObject().Sum(property => CountNodes(property.Value)),
        JsonValueKind.Array => 1 + element.EnumerateArray().Sum(CountNodes),
        _ => 1
    };

    static bool MatchesSystemTextJson(BenchmarkJson result, JsonElement expected) => result switch
    {
        BenchmarkJsonObject value => expected.ValueKind == JsonValueKind.Object
            && value.Members.Count == expected.EnumerateObject().Count()
            && value.Members.All(member => expected.TryGetProperty(member.Key, out var expectedValue)
                && MatchesSystemTextJson(member.Value, expectedValue)),
        BenchmarkJsonArray value => expected.ValueKind == JsonValueKind.Array
            && value.Elements.Count == expected.GetArrayLength()
            && value.Elements.Select((element, index) => MatchesSystemTextJson(element, expected[index])).All(it => it),
        BenchmarkJsonBoolean value => expected.ValueKind is JsonValueKind.True or JsonValueKind.False
            && value.Value == expected.GetBoolean(),
        BenchmarkJsonNull => expected.ValueKind == JsonValueKind.Null,
        BenchmarkJsonNumber value => expected.ValueKind == JsonValueKind.Number
            && string.Equals(value.Value, expected.GetRawText(), StringComparison.Ordinal),
        BenchmarkJsonString value => expected.ValueKind == JsonValueKind.String
            && string.Equals(value.Value, expected.GetString(), StringComparison.Ordinal)
    };
}
