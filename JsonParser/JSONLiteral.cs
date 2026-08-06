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
/// Represents a literal value in JSON:
/// A string, number, boolean, or null value
/// </summary>
public class JSONLiteral : IJSONValue
{
    /// <summary>
    /// A string representation of the value
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// The type the value is
    /// </summary>
    public LiteralType ValueType { get; set; }

    public JSONLiteral(LiteralType type)
        => ValueType = type;

    public JSONLiteral(string value, LiteralType type)
    {
        Value = value;
        ValueType = type;
    }

    /// <summary>
    /// Returns Value cast as the appropriate type.
    /// </summary>
    /// <returns></returns>
    public object? Get()
    {
        switch (ValueType)
        {
            case LiteralType.String:
                return Value ?? throw new InvalidOperationException();

            case LiteralType.Number:
                return Convert.ToDouble(Value ?? throw new InvalidOperationException());

            case LiteralType.Boolean:
                return (Value ?? throw new InvalidOperationException()).ToLower() == "true";

            default:
                return null;
        }
    }

    public IJSONValue this[string key]
    {
        get => throw new NotImplementedException("Cannot access JSONArray by string.");
        set => throw new NotImplementedException("Cannot access JSONArray by string.");
    }

    public IJSONValue this[int i]
    {
        get => throw new NotImplementedException("Cannot access JSONArray by string.");
        set => throw new NotImplementedException("Cannot access JSONArray by string.");
    }

    /// <summary>
    /// Returns the Value property
    /// </summary>
    /// <returns>this.Value</returns>
    public override string? ToString() => Value;

    public string ToJSON() => throw new NotImplementedException();
}
/// <summary>
	/// A list of the types a JSON literal value can be
	/// </summary>
	public enum LiteralType
{
    String,
    Number,
    Boolean,
    Null
}
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
/// <summary>
/// Represents a JSON value
/// </summary>
public interface IJSONValue
{
    /// <summary>
    /// Allows key indexing without downcasting to JSONObject
    /// </summary>
    /// <param name="key">The key to index by</param>
    /// <returns>Throws an exception unless overridden by JSONObject</returns>
    IJSONValue this[string key] { get; set; }

    /// <summary>
    /// Allows indexing without downcasting to JSONArray
    /// </summary>
    /// <param name="i">The integer to index by</param>
    /// <returns>Throws an exception unless overridden by JSONArray</returns>
    IJSONValue this[int i] { get; set; }
}

/// <summary>
/// Represents a JSON array:
/// A collection of unnamed (anonymous) JSON values
/// </summary>
public class JSONArray : IJSONValue
{
    /// <summary>
    /// All the JSON values
    /// </summary>
    public List<IJSONValue> Elements { get; set; }

    public JSONArray() => Elements = [];

    public JSONArray(IEnumerable<IJSONValue> elements)
    {
        Elements = [];
        if (elements is not null) foreach (var e in elements) Elements.Add(e);
    }

    public IJSONValue this[string key]
    {
        get => throw new NotImplementedException("Cannot access JSONArray by string.");
        set => throw new NotImplementedException("Cannot access JSONArray by string.");
    }

    /// <summary>
    /// Makes Elements directly accessable
    /// </summary>
    /// <param name="i">The index of the IJSONValue</param>
    /// <returns>The IJSONValue at index i</returns>
    public IJSONValue this[int i]
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

    public string ToJSON() => throw new NotImplementedException();
}
/// <summary>
/// Maps IJSONValue objects onto other objects
/// </summary>
static class JSONMap
{
    /// <summary>
    /// Maps a JSONObject onto an object which can be cast as an appropriate type
    /// </summary>
    /// <param name="T">The type to which the returned object will be cast</param>
    /// <param name="toMap">The JSONObject to map onto the object</param>
    /// <returns>An object of type T containing the JSON information</returns>
    static object? MapObject(Type T, JSONObject toMap)
    {
        if (T.IsSubclassOf(typeof(IDictionary)))
        {
            //Create an instance of the object as an IDictionary
            var toReturn = Activator.CreateInstance(T) as IDictionary
                ?? throw new InvalidOperationException($"Cannot create {T}.");

            //Treat the object like a dictionary, and add each element to it as a key, value pair.
            foreach (var p in toMap.Pairs)
            {
                toReturn.Add(p.Key, MapValue(T.GenericTypeArguments[1], p.Value));
            }

            return toReturn;
        }
        else if (T.IsClass)
        {
            //Create an instance of the object
            var toReturn = Activator.CreateInstance(T)
                ?? throw new InvalidOperationException($"Cannot create {T}.");

            //Loop through all the properties of the type
            foreach (var p in T.GetProperties())
            {
                //If the JSONObject contains information for that property,
                //set the value of the return object's property to that information.
                if (toMap.Pairs.ContainsKey(p.Name))
                {
                    p.SetValue(toReturn, MapValue(p.PropertyType, toMap[p.Name]));
                }
            }

            //Loop through all the fields of the type
            foreach (var f in T.GetFields())
            {
                //If the JSONObject contains information for that field,
                //set the value of the return object's field to that information.
                if (toMap.Pairs.ContainsKey(f.Name))
                {
                    f.SetValue(toReturn, MapValue(f.FieldType, toMap[f.Name]));
                }
            }

            return toReturn;
        }

        return null;
    }

    /// <summary>
    /// Maps a JSONArray onto an object which can be cast as an appropriate type
    /// </summary>
    /// <param name="T">The type to which the returned object will be cast</param>
    /// <param name="toMap">The JSONArray to map onto the object</param>
    /// <returns>An object of type T containing the JSON information</returns>
    static object MapArray(Type T, JSONArray toMap)
    {
        //If the type isn't an array or IList then the JSONArray can't be mapped onto it
        if (!T.IsArray || T.GetInterface("System.Collections.IList") is null)
        {
            throw new ArgumentException($"{T} can't map JSONArray.");
        }
        else
        {
            //If T is an array, create a new ArrayList, otherwise create a new IList of type T
            var toReturnList = T.IsArray
                ? new ArrayList()
                : Activator.CreateInstance(T) as IList
                    ?? throw new InvalidOperationException($"Cannot create {T}.");

            var elementType = T.GetElementType()
                ?? throw new InvalidOperationException($"{T} has no element type.");

            //Loop through all the elements of the array, and populate toReturnList with the mapped values of those elements
            foreach (var e in toMap.Elements)
            {
                toReturnList.Add(MapValue(elementType, e));
            }

            //If T is an array, we need to cast the list as an array, otherwise we can return the list
            if (T.IsArray)
            {
                //Create an instance of an array of the appropriate type
                var c = toReturnList.Count;
                var toReturn = Array.CreateInstance(elementType, c);

                //Loop through toReturnList and add each element to the array
                for (int i = 0; i < c; i++)
                {
                    toReturn.SetValue(toReturnList[i], i);
                }

                return toReturn;
            }
            else return toReturnList;
        }
    }

    /// <summary>
    /// Maps a JSONLiteral onto an object which can be cast as an appropriate type
    /// </summary>
    /// <param name="T">The type to which the returned object will be cast</param>
    /// <param name="toMap">The JSONLiteral to map onto the object</param>
    /// <returns>An object of type T containing the JSON information</returns>
    static object? MapLiteral(Type T, JSONLiteral toMap)
    {
        //If the literal is a null, return null IF T is nullable, otherwise throw an exception
        if (toMap.ValueType == LiteralType.Null)
        {
            if (T.IsClass || Nullable.GetUnderlyingType(T) != null) return null;
            else throw new ArgumentException($"{T} can't be null.");
        }
        //If toMap isn't a literal, toMap.Get() return an object cast as the appropriate type.
        else return toMap.Get();
    }

    /// <summary>
    /// Maps a IJSONValue onto an object which can be cast as an appropriate type
    /// </summary>
    /// <param name="T">The type to which the returned object will be cast</param>
    /// <param name="toMap">The IJSONValue to map onto the object</param>
    /// <returns>An object of type T containing the JSON information</returns>
    public static object? MapValue(Type T, IJSONValue toMap)
        => toMap switch
        {
            JSONObject jsonObject => MapObject(T, jsonObject),
            JSONArray jsonArray => MapArray(T, jsonArray),
            JSONLiteral jsonLiteral => MapLiteral(T, jsonLiteral),
            _ => throw new ArgumentException("Cannot map vanilla IJSONValue.")
        };
}
