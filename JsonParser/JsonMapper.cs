// The MIT License
// Copyright (c) 2014 Ian Wold
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;

namespace Marimo.Parser;

/// <summary>Maps values using the fixed upstream sample's supported CLR types.</summary>
static class JsonMapper
{
    static object? MapObject(Type type, JsonObject toMap)
    {
        if (!type.IsClass) return null;

        var mapped = Activator.CreateInstance(type);
        foreach (var property in
            from property in type.GetProperties()
            where toMap.Pairs.ContainsKey(property.Name)
            select property)
        {
            property.SetValue(mapped, MapValue(property.PropertyType, toMap[property.Name]));
        }

        foreach (var field in
            from field in type.GetFields()
            where toMap.Pairs.ContainsKey(field.Name)
            select field)
        {
            field.SetValue(mapped, MapValue(field.FieldType, toMap[field.Name]));
        }

        return mapped;
    }

    static Array MapArray(Type type, JsonArray toMap)
    {
        if (!type.IsArray || type.GetInterface("System.Collections.IList") is null)
        {
            throw new ArgumentException($"{type} can't map JsonArray.");
        }

        // IsArray guarantees the element type exists.
        var elementType = type.GetElementType()!;
        var mapped = Array.CreateInstance(elementType, toMap.Elements.Count);
        for (var i = 0; i < toMap.Elements.Count; i++)
        {
            mapped.SetValue(MapValue(elementType, toMap.Elements[i]), i);
        }

        return mapped;
    }

    static object? MapLiteral(Type type, JsonLiteral toMap) =>
        toMap.ValueType != LiteralType.Null
            ? toMap.Get()
        : type.IsClass || Nullable.GetUnderlyingType(type) is not null
            ? null
        : throw new ArgumentException($"{type} can't be null.");

    /// <summary>Dispatches mapping without introducing numeric conversions absent upstream.</summary>
    internal static object? MapValue(Type type, IJsonValue toMap) => toMap switch
    {
        JsonObject it => MapObject(type, it),
        JsonArray it => MapArray(type, it),
        JsonLiteral it => MapLiteral(type, it),
        _ => throw new ArgumentException("Cannot map vanilla IJSONValue.", nameof(toMap))
    };
}
