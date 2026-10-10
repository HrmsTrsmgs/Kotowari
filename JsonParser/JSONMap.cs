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
