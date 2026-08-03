using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core
{
    public class OneOrMoreParserのテスト
    {
        [Fact]
        public void 一つ目の要素のパースに失敗したら失敗です()        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (isSuccess, _, _) = tested.Parse(new Cursol("b"));

            isSuccess.IsFalse();
        }

        [Fact]
        public void 一つ目の要素のパースに成功したら成功です()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (isSuccess, _, _) = tested.Parse(new Cursol("a"));

            isSuccess.IsTrue();
        }
        [Fact]
        public void 一つ目の要素のパースに成功したら一つ目の要素のみが得られます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, _, parsed) = tested.Parse(new Cursol("a"));

            parsed.Count().Is(1);
            parsed.ElementAt(0).Is('a');
        }

        [Fact]
        public void 一つ目の要素のパースに成功したらカーソルは進みます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, cursol, _) = tested.Parse(new Cursol("a"));

            cursol.Index.Is("a".Length);
        }

        [Fact]
        public void 二つ目の要素のパースに成功したら二つ分カーソルは進みます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, cursol, _) = tested.Parse(new Cursol("aa"));

            cursol.Index.Is("aa".Length);
        }

        [Fact]
        public void 二つ目の要素のパースに成功したら二つ目の要素もパースされます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, _, parsed) = tested.Parse(new Cursol("aa"));

            parsed.Count().Is(2);
            parsed.ElementAt(0).Is('a');
            parsed.ElementAt(1).Is('a');
        }

        [Fact]
        public void 二つ目の要素のパースに失敗しても全体としては成功です()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (isSuccess, _, _) = tested.Parse(new Cursol("ab"));

            isSuccess.IsTrue();
        }

        [Fact]
        public void 二つ目の要素のパースに失敗したら一つ分カーソルは進みます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, cursol, _) = tested.Parse(new Cursol("ab"));

            cursol.Index.Is("a".Length);
        }

        [Fact]
        public void 二つ目の要素のパースに失敗したらパース結果は一文字分です()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, _, parsed) = tested.Parse(new Cursol("ab"));

            parsed.Count().Is(1);
            parsed.ElementAt(0).Is('a');
        }
    }
}
