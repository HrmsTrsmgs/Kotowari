using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Marimo.Kotowari.Tests.Core
{
    public class WordParserのテスト
    {
        [Fact]
        public void ParseAsyncは指定した単語を読み込みに成功します()
        {
            var cursol = new Cursol("public");
            var tested = new WordParser("public");

            var result = tested.Parse(cursol);

            result.isSuccess.IsTrue();
        }

        [Fact]
        public void ParseAsyncは指定していない単語を読み込みに失敗します()
        {
            var cursol = new Cursol("publi");
            var tested = new WordParser("public");

            var result = tested.Parse(cursol);

            result.isSuccess.IsFalse();
        }
        [Fact]
        public void ParseAsyncは指定した単語を読み込みます()
        {
            var cursol = new Cursol("public");
            var tested = new WordParser("public");

            var result = tested.Parse(cursol);

            result.parsed.Is("public");
        }


        [Fact]
        public void ParseAsyncは読み込みに成功した場合に単語の長さだけ進んだカーソルを返します()
        {
            var cursol = new Cursol("void");
            var tested = new WordParser("void");

            var result = tested.Parse(cursol);

            result.cursol.Index.Is(4);
        }

        [Fact]
        public void ParseAsyncは読み込みに失敗した場合には進んでいないカーソルを返します()
        {
            var cursol = new Cursol("publi");
            var tested = new WordParser("public");

            var result = tested.Parse(cursol);

            result.cursol.Index.Is(0);
        }

        [Fact]
        public void ParseAsyncは単語前のスペースを読み飛ばします()
        {
            var cursol = new Cursol(" public");
            var tested = new WordParser("public");

            var result = tested.Parse(cursol);

            result.cursol.Index.Is(7);
        }

        [Fact]
        public void ParseAsyncは単語後のスペースを読み飛ばします()
        {
            var cursol = new Cursol("public ");
            var tested = new WordParser("public");

            var result = tested.Parse(cursol);

            result.cursol.Index.Is(7);
        }
    }
}
