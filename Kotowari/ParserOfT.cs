using Marimo.Kotowari.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari;

public abstract class Parser<T> : Parser
{
    /// <summary>指定位置から解析し、成否、解析後のカーソル、および成功時の値を返します。</summary>
    /// <param name="cursol">解析を開始する文書と位置。</param>
    /// <returns>成功値または失敗を表す結果。結果オブジェクトはこの公開入口で生成します。</returns>
    public ParseResult<T> Parse(Cursol cursol)
        => TryParseCore(cursol, out var next, out var parsed)
            ? ParseResult<T>.Success(next, parsed)
            : ParseResult<T>.Failure(next);

    /// <summary>派生実装から任意の結果型の子を、結果オブジェクトを生成せずに解析します。</summary>
    /// <typeparam name="TChild">子の成功値の型。</typeparam>
    /// <param name="parser">解析する子パーサー。</param>
    /// <param name="start">子の解析を開始する文書と位置。</param>
    /// <param name="next">子が返した解析後のカーソル。</param>
    /// <param name="parsed">成功時の値。失敗時は未取得です。成功時のnull許容性は<typeparamref name="TChild"/>に従います。</param>
    /// <returns>子の解析が成功した場合はtrue、それ以外はfalse。</returns>
    protected static bool TryParseChild<TChild>(Parser<TChild> parser, Cursol start, out Cursol next, [MaybeNullWhen(false)] out TChild parsed)
        => parser.TryParseCore(start, out next, out parsed);

    /// <summary>
    /// 元のパーサーを変更せずに、成功後に使用する空白規則を指定します。
    /// </summary>
    /// <typeparam name="TWhiteSpace">空白規則が返す値の型。</typeparam>
    /// <param name="whiteSpace">字句の成功後に使用する空白規則。</param>
    /// <returns>指定した空白規則を使用するパーサー。</returns>
    public Parser<T> WithWhiteSpace<TWhiteSpace>(Parser<TWhiteSpace> whiteSpace)
        => new TrailingWhiteSpaceParser<TWhiteSpace>(this, whiteSpace);

    sealed class TrailingWhiteSpaceParser<TWhiteSpace>(Parser<T> parser, Parser<TWhiteSpace> whiteSpace) : Parser<T>
    {
        protected internal override bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed)
        {
            if (!parser.TryParseCore(start, out next, out parsed))
            {
                return false;
            }

            next = WhiteSpaceSkipper.Skip(whiteSpace, next);
            return true;
        }
    }

    /// <summary>
    /// 指定位置からの解析を実装します。
    /// </summary>
    /// <param name="start">解析を開始する文書と位置。</param>
    /// <param name="next">解析後のカーソル。失敗時に入力を戻すかどうかは各パーサーの契約に従います。</param>
    /// <param name="parsed">成功時の値。失敗時は未取得です。成功時のnull許容性は<typeparamref name="T"/>に従います。</param>
    /// <returns>解析が成功した場合はtrue、それ以外はfalse。</returns>
    /// <remarks>
    /// <para>
    /// 内部コンビネーターは子のTryParseCoreを直接呼び、公開Parseの出口でだけ結果オブジェクトを生成します。
    /// protected internalは、内部呼び出しと外部assemblyからの派生実装の両方を許すためです。
    /// 利用側の解析入口は公開のParseのままです。外部assemblyの派生クラスでは
    /// protected overrideで実装します。同一assembly内のoverrideはprotected internalにそろえます。
    /// 外部assemblyから任意の子を合成する場合はTryParseChildを使います。
    /// 以下は旧ParseCore署名で2026-10-05に測定したPGO対策の記録です。
    /// bool/out署名への変更後の性能は未測定ですが、既存の内部呼び出し位置は維持します。
    /// </para>
    /// <para>
    /// この呼び分けは、Dynamic PGOが収集する型profileを実際の呼び出し位置に対応させるためです。
    /// 測定当時のParseは非virtualの共通入口で、virtualなParseCoreへ委譲するだけでしたが、
    /// 同じ閉じたジェネリック型のParseを通ると、異なる用途の子パーサーの実行時型が
    /// 同じvirtual呼び出し位置のprofileへ集まります。例えばParser&lt;char&gt;.Parseには
    /// CharParser、OrParser&lt;char&gt;、ConditionalCharParserなどの呼び出しが集まります。
    /// JITがそのprofileを使ってParseを呼び出し側へインライン化すると、
    /// 共通入口で選ばれた型が、その呼び出し側で実際に渡される型と一致しないことがあります。
    /// この混在により型ガードと展開先が用途に合わなくなることが、調査で得た最有力の原因仮説です。
    /// </para>
    /// <para>
    /// 2026-10-05の調査では、同一実装・同一JSON入力・PGO有効でも、
    /// プロセスごとに約173msと約250msの速度帯がありました。
    /// 遅い起動のZeroOrMoreParser&lt;char&gt;のTier 1コードには、共通入口由来の
    /// CharParser用の型ガードと直接呼び出しがあり、不一致ならvirtual呼び出しへ進みました。
    /// このJSON構成でZeroOrMoreが反復する相手は、文字列ではOrParser&lt;char&gt;、
    /// 数字列ではConditionalCharParserなので、そのCharParser用の枝は使われません。
    /// 速い起動ではOrParser&lt;char&gt;用のガードと展開があり、文字列の経路で使われました。
    /// 生の型profile分布は未採取ですが、このガード対象と展開内容の違いは生成コードで確認しました。
    /// </para>
    /// <para>
    /// 当時、各コンビネーターがParseCoreを直接呼ぶと、そのvirtual呼び出し位置で型profileを収集できました。
    /// 実際にZeroOrMoreのInstrumented Tier0へ型profile計測helperが入り、Tier 1のガード対象は
    /// OrParser&lt;char&gt;またはConditionalCharParserへ変わりました。採取した両方の選択で
    /// 約156～163msの速い帯に収まり、選択される型や生成コードが毎回同じになる必要はありませんでした。
    /// 直接呼び出しでもvirtual dispatchは残り得ます。型が一致する枝の直接呼び出しや
    /// インライン化はJITの判断なので、単にメソッド呼び出しを一つ削ることや、
    /// 手動で処理を複製・インライン化することを目的とした変更ではありません。
    /// </para>
    /// <para>
    /// 採用判断は、.NET 10.0.12、Windows 11、i7-13700Hでの実測に基づきます。
    /// 既存の4,172,062バイトのJSONを全文解析して100,001ノードの結果を生成し、
    /// 変更前後で入力消費と結果の木が一致することを測定外で確認しました。
    /// ファイル読み込みは事前に済ませ、解析・結果生成1回の時間を測っています。
    /// PGO有効、Releaseの固定DLL、各版8回の新規プロセス起動、Warmup 16・Actual 8反復、
    /// 各反復4解析、診断なし・外れ値除去なしの条件で、起動平均の平均は
    /// 271.41msから156.71msへ約42.3%短縮し、起動間CVは8.16%から1.18%へ減りました。
    /// CVは起動平均の標本標準偏差を平均で割った値です。解析1回の割り当ては両版とも約60.33MBでした。
    /// </para>
    /// <para>
    /// 内部24呼び出し位置をまとめて変更したため、改善量のすべてをZeroOrMore一箇所へ帰属させません。
    /// Cursol.Currentのbounds checkは両版に残り、採取した対象メソッドでOSRの影響は観測しませんでした。
    /// PGO無効化やJITのインライン化・Tier移行閾値の変更で、遅い側へ性能をそろえてはいません。
    /// Warmupを増やすだけでも変更前の起動間変動は消えませんでした。
    /// 総解析回数だけで安定と判断せず、Tier 1がどの時点までのprofileで生成されるかを確認します。
    /// Tier 1生成後に200回解析しても、そのコードが全200回の分布を使って再生成されたとはいえません。
    /// この入力・環境では改善しましたが、別入力・別ランタイムやまれな遅い起動までの保証はありません。
    /// </para>
    /// <para>
    /// 内部呼び出しを共通のParseへ戻すリファクタリングでは、型profileの再混在と性能を再検証します。
    /// 現在のParseはTryParseCoreの成否と出力から公開結果を生成します。
    /// 内部経路でも値、カーソル、成否、例外の契約は共通です。
    /// 将来Parseに検証などの振る舞いを追加する場合は、内部の直接呼び出しにも必要な契約を確認してください。
    /// 再評価はPGO有効・同一入力・同一条件の複数の新規プロセスで行い、
    /// 診断付きの生成コード確認と、診断なしの速度比較を分けます。
    /// 詳細な条件、生成コード、版、結果はリポジトリ内の
    /// Kotowari.Benchmarks/Results/2026-10-05-pgo-codegen.mdと
    /// Kotowari.Benchmarks/Results/2026-10-05-pgo-callsite.md、および各報告の証拠JSONに記録しています。
    /// </para>
    /// </remarks>
    protected internal abstract bool TryParseCore(Cursol start, out Cursol next, [MaybeNullWhen(false)] out T parsed);
}
