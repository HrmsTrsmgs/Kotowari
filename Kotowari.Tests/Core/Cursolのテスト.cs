using FluentAssertions;
using Marimo.Kotowari;
using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Marimo.Kotowari.Tests.Core;

public class Cursolのテスト
{
    [Fact]
    public void 初期状態のIndexは0です()
    {
        var tested = new Cursol("");
        tested.Index.Should().Be(0);
    }

    [Fact]
    public void 初期状態のTextはコンストラクタで指定したものです()
    {
        var text = "ABC";
        var tested = new Cursol(text);
        tested.Text.Should().Equal(text);
    }

    [Fact]
    public void GoFowardでIndexが進んだCursolが手に入ります()
    {
        var tested = new Cursol("ABC");
        tested = tested.GoFoward(2);
        tested.Index.Should().Be(2);
    }
    [Fact]
    public void GoFowardは前の状態と比較して進んだ値を指定します()
    {
        var tested = new Cursol("ABC");
        tested = tested.GoFoward(1);
        tested = tested.GoFoward(1);
        tested.Index.Should().Be(2);
    }
    [Fact]
    public void GoFowardは最後の文字の一個先までしか進むことができません()
    {
        var tested = new Cursol("ABC");
        tested = tested.GoFoward(4);
        tested.Index.Should().Be(3);
    }

    [Fact]
    public void GoFowardは元の値をCursolを変更しません()
    {
        var tested = new Cursol("ABC");
        tested.GoFoward(1);
        tested.Index.Should().Be(0);
    }

    [Fact]
    public void Copyは同じTextを持つCursolを返します()
    {
        var tested = new Cursol("ABC");
        tested.Copy().Text.Should().Equal("ABC");
    }

    [Fact]
    public void Copyは同じIndexを持つCursolを返します()
    {
        var tested = new Cursol("ABC");
        tested = tested.GoFoward(1);
        tested.Copy().Index.Should().Be(1);
    }

    [Fact]
    public void 同じ文書の同じ位置にあるCopyは等価です()
    {
        var tested = new Cursol("ABC").GoFoward(1);

        var copied = tested.Copy();

        (copied == tested).Should().BeTrue();
    }

    [Fact]
    public void 同じ文書でも異なる位置にあるCursolは等価ではありません()
    {
        var tested = new Cursol("ABC");

        var moved = tested.GoFoward(1);

        (moved != tested).Should().BeTrue();
    }

    [Fact]
    public void 同じ内容から別々に作ったCursolは異なる文書を指します()
    {
        var first = new Cursol("ABC");
        var second = new Cursol("ABC");

        (first != second).Should().BeTrue();
    }
}
