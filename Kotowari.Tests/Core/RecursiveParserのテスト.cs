using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core
{
    public class RecursiveParserのテスト
    {
        [Fact]
        public void ParseAsyncは内部パーサーと同じように成功します()
        {
            var cursol = new Cursol("public");
            var parser = new CharParser('p');
            var tested = new RecursiveParser<char>(() => parser);
            (tested.Parse(cursol)).IsStructuralEqual(parser.Parse(cursol));
        }
        [Fact]
        public void ParseAsyncは内部パーサーと同じように失敗します()
        {
            var cursol = new Cursol("public");
            var parser = new CharParser('a');
            var tested = new RecursiveParser<char>(() => parser);
            (tested.Parse(cursol)).IsStructuralEqual(parser.Parse(cursol));
        }
    }
}
