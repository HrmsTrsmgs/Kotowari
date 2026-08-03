using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core
{
    public class ZeroOrMoreParserのテスト
    {
        [Fact]
        public void 一つ目の要素のパースに失敗しても成功です()        {
            var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

            var (isSuccess, _, _) = tested.Parse(new Cursol("b"));

            isSuccess.IsTrue();
        }

        [Fact]
        public void 一つ目の要素のパースに失敗したらカーソルは進みません()
        {
            var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

            var (_, cursol, _) = tested.Parse(new Cursol("b"));

            cursol.Index.Is(0);
        }

        [Fact]
        public void 一つ目の要素のパースに失敗したら結果は空です()
        {
            var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

            var (_, _, parsed) = tested.Parse(new Cursol("b"));

            parsed.Count().Is(0);
        }

        [Fact]
        public void 一つ目の要素のパースに成功したら成功です()
        {
            var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

            var (isSuccess, _, _) = tested.Parse(new Cursol("a"));

            isSuccess.IsTrue();
        }

        [Fact]
        public void 一つ目の要素のパースに成功したらカーソルは進みます()
        {
            var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

            var (_, cursol, _) = tested.Parse(new Cursol("a"));

            cursol.Index.Is("a".Length);
        }

        [Fact]
        public void 一つ目の要素のパースに成功したら一つ目の要素のみが得られます()
        {
            var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

            var (_, _, parsed) = tested.Parse(new Cursol("a"));

            parsed.Count().Is(1);
            parsed.ElementAt(0).Is('a');
        }

        [Fact]
        public void 二つ目の要素のパースに成功したら二つ分カーソルは進みます()
        {
            var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

            var (_, cursol, _) = tested.Parse(new Cursol("aa"));

            cursol.Index.Is("aa".Length);
        }

        [Fact]
        public void 二つ目の要素のパースに成功したら二つ目の要素もパースされます()
        {
            var tested = new ZeroOrMoreParser<char>(new CharParser('a'));

            var (_, _, parsed) = tested.Parse(new Cursol("aa"));

            parsed.Count().Is(2);
            parsed.ElementAt(0).Is('a');
            parsed.ElementAt(1).Is('a');
        }
    }
}
