//The MIT License

//Copyright (c) 2014 Ian Wold

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace Marimo.Parser;

/// <summary>
/// Represents a JSON value
/// </summary>
public interface IJsonValue
{
    /// <summary>Returns the upstream sample's JSON representation.</summary>
    string ToJson();

    /// <summary>
    /// Allows key indexing without downcasting to JsonObject
    /// </summary>
    /// <param name="key">The key to index by</param>
    /// <returns>Throws an exception unless overridden by JsonObject</returns>
    IJsonValue this[string key] { get; set; }

    /// <summary>
    /// Allows indexing without downcasting to JsonArray
    /// </summary>
    /// <param name="i">The integer to index by</param>
    /// <returns>Throws an exception unless overridden by JsonArray</returns>
    IJsonValue this[int i] { get; set; }
}
