// The MIT License
// Copyright (c) 2014 Ian Wold
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Linq;

namespace Marimo.Parser;

static class JsonTextExtensions
{
    extension(string self)
    {
        /// <summary>Uses the upstream sample's escaping, including escaped forward slashes.</summary>
        internal string ToJsonString() =>
            string.Concat(from character in self
                          select character switch
                          {
                              '/' => "\\/",
                              '\\' => "\\\\",
                              '\b' => "\\b",
                              '\f' => "\\f",
                              '\n' => "\\n",
                              '\r' => "\\r",
                              '\t' => "\\t",
                              '"' => "\\\"",
                              _ => character.ToString()
                          });

        /// <summary>Indents every line while retaining the upstream CRLF representation.</summary>
        internal string Tabify() =>
            string.Concat(from line in self.Split('\n') select $"\t{line}\n")[..^1];
    }
}
