// Verifies the Kotowari JSON benchmark input and emits a comparable tree fingerprint.
// Copy this file to the repository root as VerifyJson.cs, then run:
// dotnet run --file <repository-root>/VerifyJson.cs -c Release -- <input> <absolute-result.json>
#:project ./JsonParser/JsonParser.csproj
#:property PublishAot=false

using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using Marimo.Parser;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length != 2)
    throw new ArgumentException("Expected the benchmark input path and an absolute result path.");

var inputPath = Path.GetFullPath(args[0]);
var resultPath = Path.GetFullPath(args[1]);
if (!Path.IsPathFullyQualified(args[1]))
    throw new ArgumentException("The result path must be absolute.", nameof(args));
if (File.Exists(resultPath))
    throw new IOException($"The result file already exists: {resultPath}");

var text = File.ReadAllText(inputPath);
var rootParserName = typeof(Json).GetField("Document", BindingFlags.Static | BindingFlags.NonPublic) is not null
    ? "Document"
    : "JObject";
var rootParser = typeof(Json).GetField(rootParserName, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as Parser<JsonObject>
    ?? throw new InvalidOperationException($"Kotowari root parser field '{rootParserName}' was not found.");
var parseResult = rootParser.Parse(new Cursol(text));
if (!parseResult.IsSuccess)
    throw new InvalidOperationException("Kotowari root parser failed.");

var consumed = parseResult.Cursol.Index;
if (consumed != text.Length)
    throw new InvalidOperationException($"Kotowari root parser consumed {consumed}/{text.Length} characters.");

IJsonValue rootValue = parseResult.Parsed;
var publicValue = Json.Parse(text);
var rootTree = SerializeTree(rootValue);
var publicTree = SerializeTree(publicValue);
if (!rootTree.Bytes.AsSpan().SequenceEqual(publicTree.Bytes))
    throw new InvalidOperationException("Json.Parse and the root parser returned different trees.");

var output = JsonSerializer.Serialize(
    new { Characters = text.Length, Consumed = consumed, Nodes = rootTree.Nodes, TreeHash = rootTree.Hash },
    new JsonSerializerOptions { WriteIndented = true });
var outputDirectory = Path.GetDirectoryName(resultPath)
    ?? throw new InvalidOperationException("The result path has no parent directory.");
Directory.CreateDirectory(outputDirectory);
File.WriteAllText(resultPath, output + Environment.NewLine);

static (byte[] Bytes, int Nodes, string Hash) SerializeTree(IJsonValue root)
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream))
    {
        var nodes = 0;
        WriteNode(writer, root, ref nodes);
        writer.Flush();
        var bytes = stream.ToArray();
        return (bytes, nodes, Convert.ToHexString(SHA256.HashData(bytes)));
    }
}

static void WriteNode(BinaryWriter writer, IJsonValue value, ref int nodes)
{
    nodes++;
    switch (value)
    {
        case JsonObject jsonObject:
            writer.Write((byte)'O');
            writer.Write(jsonObject.Pairs.Count);
            foreach (var pair in jsonObject.Pairs)
            {
                WriteString(writer, pair.Key);
                WriteNode(writer, pair.Value, ref nodes);
            }
            break;

        case JsonArray jsonArray:
            writer.Write((byte)'A');
            writer.Write(jsonArray.Elements.Count);
            foreach (var element in jsonArray.Elements)
                WriteNode(writer, element, ref nodes);
            break;

        case JsonLiteral literal:
            writer.Write((byte)'L');
            WriteString(writer, literal.ValueType.ToString());
            writer.Write(literal.Value is not null);
            if (literal.Value is not null)
                WriteString(writer, literal.Value);
            break;

        default:
            throw new InvalidOperationException($"Unsupported JSON node type: {value.GetType().FullName}");
    }
}

static void WriteString(BinaryWriter writer, string value)
{
    writer.Write(value.Length);
    foreach (var character in value)
        writer.Write((ushort)character);
}
