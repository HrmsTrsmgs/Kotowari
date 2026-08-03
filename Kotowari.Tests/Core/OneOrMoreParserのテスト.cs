using FluentAssertions;
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

            isSuccess.Should().BeFalse();
        }

        [Fact]
        public void 一つ目の要素のパースに成功したら成功です()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (isSuccess, _, _) = tested.Parse(new Cursol("a"));

            isSuccess.Should().BeTrue();
        }
        [Fact]
        public void 一つ目の要素のパースに成功したら一つ目の要素のみが得られます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, _, parsed) = tested.Parse(new Cursol("a"));

            parsed.Should().ContainSingle();
            parsed.ElementAt(0).Should().Be('a');
        }

        [Fact]
        public void 一つ目の要素のパースに成功したらカーソルは進みます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, cursol, _) = tested.Parse(new Cursol("a"));

            cursol.Index.Should().Be("a".Length);
        }

        [Fact]
        public void 二つ目の要素のパースに成功したら二つ分カーソルは進みます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, cursol, _) = tested.Parse(new Cursol("aa"));

            cursol.Index.Should().Be("aa".Length);
        }

        [Fact]
        public void 二つ目の要素のパースに成功したら二つ目の要素もパースされます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, _, parsed) = tested.Parse(new Cursol("aa"));

            parsed.Count().Should().Be(2);
            parsed.ElementAt(0).Should().Be('a');
            parsed.ElementAt(1).Should().Be('a');
        }

        [Fact]
        public void 二つ目の要素のパースに失敗しても全体としては成功です()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (isSuccess, _, _) = tested.Parse(new Cursol("ab"));

            isSuccess.Should().BeTrue();
        }

        [Fact]
        public void 二つ目の要素のパースに失敗したら一つ分カーソルは進みます()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, cursol, _) = tested.Parse(new Cursol("ab"));

            cursol.Index.Should().Be("a".Length);
        }

        [Fact]
        public void 二つ目の要素のパースに失敗したらパース結果は一文字分です()
        {
            var tested = new OneOrMoreParser<char>(new CharParser('a'));

            var (_, _, parsed) = tested.Parse(new Cursol("ab"));

            parsed.Should().ContainSingle();
            parsed.ElementAt(0).Should().Be('a');
        }
    }
}
