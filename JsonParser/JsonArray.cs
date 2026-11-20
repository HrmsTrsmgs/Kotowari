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
/// Represents a JSON array:
/// A collection of unnamed (anonymous) JSON values
/// </summary>
public class JsonArray : IJsonValue
{
    /// <summary>
    /// All the JSON values
    /// </summary>
    public List<IJsonValue> Elements { get; set; }

    public JsonArray() => Elements = [];

    public JsonArray(IEnumerable<IJsonValue> elements)
    {
        Elements = [];
        if (elements is not null) foreach (var e in elements) Elements.Add(e);
    }

    public IJsonValue this[string key]
    {
        get => throw new NotImplementedException("Cannot access JSONArray by string.");
        set => throw new NotImplementedException("Cannot access JSONArray by string.");
    }

    /// <summary>
    /// Makes Elements directly accessable
    /// </summary>
    /// <param name="i">The index of the IJsonValue</param>
    /// <returns>The IJsonValue at index i</returns>
    public IJsonValue this[int i]
    {
        get
        {
            if (0 <= i && i < Elements.Count) return Elements[i];
            else throw new IndexOutOfRangeException($"{i} is out of range.");
        }
        set
        {
            if (0 <= i && i < Elements.Count) Elements[i] = value;
            else throw new IndexOutOfRangeException($"{i} is out of range.");
        }
    }

    /// <summary>Returns the array's indented JSON representation. Empty arrays are unsupported.</summary>
    public string ToJson() =>
        $"[\r\n{string.Concat(from element in Elements select $"{element.ToJson()},\r\n")[..^3].Tabify()}\r\n]";
}
