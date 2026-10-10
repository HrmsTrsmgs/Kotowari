//The MIT License

//Copyright (c) 2014 Ian Wold

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Marimo.Parser;

/// <summary>
/// Represents a JSON object:
/// A collection of JSON pairs
/// </summary>
public class JSONObject : IJSONValue
{
    /// <summary>
    /// All the JSON pair objects
    /// </summary>
    public Dictionary<string, IJSONValue> Pairs { get; set; }

    public JSONObject() => Pairs = [];

    public JSONObject(IEnumerable<KeyValuePair<string, IJSONValue>> pairs)
    {
        Pairs = [];
        if (pairs is not null) foreach (var p in pairs) Pairs.Add(p.Key, p.Value);
    }

    /// <summary>
    /// Makes Pairs directly accessable
    /// </summary>
    /// <param name="key">The key of the IJSONValue</param>
    /// <returns>The IJSONValue at that key</returns>
    public IJSONValue this[string key]
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

    public IJSONValue this[int i]
    {
        get => throw new NotImplementedException("Cannot access JSONArray by string.");
        set => throw new NotImplementedException("Cannot access JSONArray by string.");
    }

    public string ToJSON() => throw new NotImplementedException();
}
