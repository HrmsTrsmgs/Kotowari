//The MIT License

//Copyright (c) 2014 Ian Wold

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marimo.Parser;

/// <summary>
/// Represents a JSON object:
/// A collection of JSON pairs
/// </summary>
public class JsonObject : IJsonValue
{
    /// <summary>
    /// All the JSON pair objects
    /// </summary>
    public Dictionary<string, IJsonValue> Pairs { get; set; }

    public JsonObject() => Pairs = [];

    public JsonObject(IEnumerable<KeyValuePair<string, IJsonValue>> pairs)
    {
        Pairs = [];
        if (pairs is not null) foreach (var p in pairs) Pairs.Add(p.Key, p.Value);
    }

    /// <summary>
    /// Makes Pairs directly accessable
    /// </summary>
    /// <param name="key">The key of the IJsonValue</param>
    /// <returns>The IJsonValue at that key</returns>
    public IJsonValue this[string key]
    {
        get
        {
            if (Pairs.ContainsKey(key)) return Pairs[key];
            else throw new ArgumentException($"Key not found: {key}");
        }
        set
        {
            if (Pairs.ContainsKey(key)) Pairs[key] = value;
            else throw new ArgumentException($"Key not found: {key}");
        }
    }

    /// <summary>Gets or replaces an existing value by its position in the object's pairs.</summary>
    /// <param name="i">The zero-based pair position.</param>
    /// <returns>The JSON value at that position.</returns>
    public IJsonValue this[int i]
    {
        get => i >= 0 && i < Pairs.Count
            ? Pairs.ElementAt(i).Value
            : throw new IndexOutOfRangeException($"{i} is outside the bounds of the JsonObject.");
        set => Pairs[Pairs.ElementAt(i).Key] = i >= 0 && i < Pairs.Count
            ? value
            : throw new IndexOutOfRangeException($"{i} is outside the bounds of the JsonObject.");
    }

    /// <summary>Returns the object's indented JSON representation. Empty objects are unsupported.</summary>
    public string ToJson() =>
        $"{{\r\n{string.Concat(from pair in Pairs select $"\"{pair.Key}\": {pair.Value.ToJson()},\r\n")[..^3].Tabify()}\r\n}}";
}
