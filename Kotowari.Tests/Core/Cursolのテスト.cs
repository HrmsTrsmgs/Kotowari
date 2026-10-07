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
        var tested = Cursol.FromString("");
        tested.Index.Should().Be(0);
    }

    [Fact]
    public void 初期状態のTextはコンストラクタで指定したものです()
    {
        var text = "ABC";
        var tested = Cursol.FromString(text);
        new string(tested.Text).Should().Be(text);
    }

    [Fact]
    public void GoFowardでIndexが進んだCursolが手に入ります()
    {
        var tested = Cursol.FromString("ABC");
        tested = tested.GoFoward(2);
        tested.Index.Should().Be(2);
    }
    [Fact]
    public void GoFowardは前の状態と比較して進んだ値を指定します()
    {
        var tested = Cursol.FromString("ABC");
        tested = tested.GoFoward(1);
        tested = tested.GoFoward(1);
        tested.Index.Should().Be(2);
    }
    [Fact]
    public void GoFowardは最後の文字の一個先までしか進むことができません()
    {
        var tested = Cursol.FromString("ABC");
        tested = tested.GoFoward(4);
        tested.Index.Should().Be(3);
    }

    [Fact]
    public void GoFowardは元の値をCursolを変更しません()
    {
        var tested = Cursol.FromString("ABC");
        tested.GoFoward(1);
        tested.Index.Should().Be(0);
    }

    [Fact]
    public void Copyは同じTextを持つCursolを返します()
    {
        var tested = Cursol.FromString("ABC");
        new string(tested.Copy().Text).Should().Be("ABC");
    }

    [Fact]
    public void Copyは同じIndexを持つCursolを返します()
    {
        var tested = Cursol.FromString("ABC");
        tested = tested.GoFoward(1);
        tested.Copy().Index.Should().Be(1);
    }

    [Fact]
    public void 同じ文書の同じ位置にあるCopyは等価です()
    {
        var tested = Cursol.FromString("ABC").GoFoward(1);

        var copied = tested.Copy();

        (copied == tested).Should().BeTrue();
    }

    [Fact]
    public void 同じ文書でも異なる位置にあるCursolは等価ではありません()
    {
        var tested = Cursol.FromString("ABC");

        var moved = tested.GoFoward(1);

        (moved != tested).Should().BeTrue();
    }

    [Fact]
    public void 同じ内容から別々に作ったCursolは異なる文書を指します()
    {
        var first = Cursol.FromString("ABC");
        var second = Cursol.FromString("ABC");

        (first != second).Should().BeTrue();
    }

    [Theory]
    [InlineData("ab", 'a')]
    [InlineData("éb", 'é')]
    [InlineData("あb", 'あ')]
    public void UTF8入力の先頭文字を読み取れます(string input, char first)
    {
        var tested = Cursol.FromUtf8(Encoding.UTF8.GetBytes(input));

        tested.Index.Should().Be(0);
        tested.Current.Should().Be(first);
    }

    [Fact]
    public void UTF8入力はスタック上のスライスをコピーせず読み取ります()
    {
        Span<byte> input = stackalloc byte[4];
        "xaby"u8.CopyTo(input);
        ReadOnlySpan<byte> utf8 = input[1..3];
        var tested = Cursol.FromUtf8(utf8);

        tested.Current.Should().Be('a');
        input[1] = (byte)'c';
        tested.Current.Should().Be('c');

        var moved = tested.GoFoward(1);
        moved.Index.Should().Be(1);
        moved.Current.Should().Be('b');
        input[2] = (byte)'d';
        moved.Current.Should().Be('d');
        moved.GoFoward(1).Current.Should().Be(Cursol.NullCharacter);
    }

    [Theory]
    [InlineData("ab", 1)]
    [InlineData("éb", 2)]
    [InlineData("あb", 3)]
    public void UTF8入力は一文字進むと次の文字のバイト位置になります(string input, int nextIndex)
    {
        var tested = Cursol.FromUtf8(Encoding.UTF8.GetBytes(input));

        var moved = tested.GoFoward(1);

        moved.Index.Should().Be(nextIndex);
        moved.Current.Should().Be('b');
        (moved != tested).Should().BeTrue();
        tested.Index.Should().Be(0);
        tested.Current.Should().Be(input[0]);
    }

    [Theory]
    [InlineData(1, 3, 'あ')]
    [InlineData(2, 6, 'b')]
    [InlineData(10, 7, '\0')]
    public void UTF8入力は現在位置から文字数だけ進み終端を越えません(int step, int nextIndex, char next)
    {
        var tested = Cursol.FromUtf8(Encoding.UTF8.GetBytes("aéあb")).GoFoward(1);

        var moved = tested.GoFoward(step);

        moved.Index.Should().Be(nextIndex);
        moved.Current.Should().Be(next);
    }

    [Fact]
    public void UTF8入力のCopyは同じ文書と位置を指します()
    {
        var tested = Cursol.FromUtf8(Encoding.UTF8.GetBytes("あb")).GoFoward(1);

        var copied = tested.Copy();

        (copied == tested).Should().BeTrue();
        copied.Index.Should().Be(3);
        copied.Current.Should().Be('b');
    }

    [Fact]
    public void 同じUTF8入力から別々に作ったCursolは異なる文書を指します()
    {
        var input = Encoding.UTF8.GetBytes("あb");
        var first = Cursol.FromUtf8(input);
        var second = Cursol.FromUtf8(input);

        (first != second).Should().BeTrue();
    }
}
