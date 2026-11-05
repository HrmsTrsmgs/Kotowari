// The MIT License
// Copyright (c) 2014 Ian Wold
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Text;

namespace Marimo.Parser;

/// <summary>Serializes the CLR types supported by the fixed upstream sample.</summary>
static class JsonSerializer
{
    static string SerializeObject(Type type, object toWrite)
    {
        var builder = new StringBuilder();
        if (type.IsClass)
        {
            foreach (var property in type.GetProperties())
            {
                builder.Append($"\"{property.Name}\": {SerializeValue(property.PropertyType, property.GetValue(toWrite))},\r\n");
            }

            foreach (var field in type.GetFields())
            {
                builder.Append($"\"{field.Name}\": {SerializeValue(field.FieldType, field.GetValue(toWrite))},\r\n");
            }
        }

        return $"{{\r\n{builder.ToString()[..^3].Tabify()}\r\n}}";
    }

    static string SerializeArray(object toWrite)
    {
        var builder = new StringBuilder();
        foreach (var element in (IList)toWrite)
        {
            builder.Append($"{SerializeValue(element.GetType(), element)},\r\n");
        }

        return $"[\r\n{builder.ToString()[..^3].Tabify()}\r\n]";
    }

    static string SerializeLiteral(Type type, object toWrite)
    {
        if (type.IsEquivalentTo(typeof(double))) return Convert.ToDouble(toWrite).ToString();
        if (type.IsEquivalentTo(typeof(bool))) return toWrite is true ? "true" : "false";
        if (type.IsEquivalentTo(typeof(string))) return $"\"{((string)toWrite).ToJsonString()}\"";

        throw new ArgumentException($"{type} can't be serialized as a literal JSON value.");
    }

    /// <summary>Preserves upstream type dispatch, including unsupported integral primitives.</summary>
    internal static string SerializeValue(Type type, object? toWrite) =>
        toWrite is null
            ? "null"
        : type.GetInterface("System.Collections.IList") is not null || type.IsArray
            ? SerializeArray(toWrite)
        : type.IsPrimitive || type.IsEquivalentTo(typeof(string)) || type.IsEquivalentTo(typeof(bool))
            ? SerializeLiteral(type, toWrite)
        : SerializeObject(type, toWrite);
}
