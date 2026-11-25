//The MIT License

//Copyright (c) 2014 Ian Wold

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;

namespace Marimo.Parser;

/// <summary>
/// Represents a literal value in JSON:
/// A string, number, boolean, or null value
/// </summary>
public class JsonLiteral : IJsonValue
{
    /// <summary>
    /// A string representation of the value
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// The type the value is
    /// </summary>
    public LiteralType ValueType { get; set; }

    public JsonLiteral(LiteralType type)
        => ValueType = type;

    public JsonLiteral(string value, LiteralType type)
    {
        Value = value;
        ValueType = type;
    }

    /// <summary>
    /// Returns Value cast as the appropriate type.
    /// </summary>
    /// <returns></returns>
    public object? Get() => ValueType switch
    {
        LiteralType.String => Value,
        LiteralType.Number => Convert.ToDouble(Value),
        LiteralType.Boolean => Value?.ToLower() == "true",
        _ => null
    };

    public IJsonValue this[string key]
    {
        get => throw new NotImplementedException("Cannot access JSONArray by string.");
        set => throw new NotImplementedException("Cannot access JSONArray by string.");
    }

    public IJsonValue this[int i]
    {
        get => throw new NotImplementedException("Cannot access JSONArray by string.");
        set => throw new NotImplementedException("Cannot access JSONArray by string.");
    }

    /// <summary>
    /// Returns the Value property
    /// </summary>
    /// <returns>this.Value</returns>
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Returns the literal's JSON representation, using the upstream sample's escaping.</summary>
    public string ToJson() => ValueType switch
    {
        LiteralType.String => $"\"{Value?.ToJsonString()}\"",
        LiteralType.Null => "null",
        _ => Value ?? string.Empty
    };
}
