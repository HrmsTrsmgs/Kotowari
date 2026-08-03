using FluentAssertions;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core
{
    public class OrParserのテスト
    {
        [Fact]
        public void 一つ目の要素のパースに成功したら成功です()
        {
            var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

            var (isSuccess, _, _) = tested.Parse(new Cursol("abc"));

            isSuccess.Should().BeTrue();
        }


        [Fact]
        public void 一つ目の要素のパースに成功したら一つ目の要素が返ります()
        {
            var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

            var (_, _, parsed) = tested.Parse(new Cursol("abc"));

            parsed.Should().Be("abc");
        }

        [Fact]
        public void 一つ目の要素のパースに成功したら一つ目の要素の分だけカーソルが進みます()
        {
            var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

            var (_, cursol, _) = tested.Parse(new Cursol("abc"));

            cursol.Index.Should().Be("abc".Length);
       }

        [Fact]
        public void 一つ目の要素のパースに失敗し二つ目の要素には成功したら成功です()
        {
            var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

            var (isSuccess, _, _) = tested.Parse(new Cursol("ab"));

            isSuccess.Should().BeTrue();
        }

        [Fact]
        public void 一つ目の要素のパースに失敗し二つ目の要素のパースに成功したら二つ目の要素が返ります()
        {
            var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

            var (_, _, parsed) = tested.Parse(new Cursol("abc"));

            parsed.Should().Be("abc");
        }

        [Fact]
        public void 一つ目の要素のパースに失敗し二つ目の要素には成功したら二つ目の要素の分だけカーソルが進みます()
        {
            var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

            var (_, cursol, _) = tested.Parse(new Cursol("ab"));

            cursol.Index.Should().Be("ab".Length);
        }

        [Fact]
        public void 二つとも失敗ならパースは失敗です()
        {
            var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

            var (isSuccess, _, _) = tested.Parse(new Cursol("a"));

            isSuccess.Should().BeFalse();
        }

        [Fact]
        public void 二つとも失敗ならカーソルは進みません()
        {
            var tested = new OrParser<string>(new WordParser("abc"), new WordParser("ab"));

            var (_, cursol, _) = tested.Parse(new Cursol("a"));

            cursol.Index.Should().Be(0);
        }
    }
}
