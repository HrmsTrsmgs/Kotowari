using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marimo.Kotowari.Core;

public class ParserConverter<U, T> : Parser<T>
    where U : notnull
    where T : notnull
{
    Parser<U> Parser { get; }
    Func<U, T> Converter { get; }

    public ParserConverter(Parser<U> parser, Func<U, T> converter)
    {
        Parser = parser;
        Converter = converter;
    }

    protected internal override ParseResult<T, TDocument> ParseCore<TDocument>(Cursol<TDocument> cursol)
        => Execution<TDocument>.Get(this).ParseCore(cursol);

    internal override ParserExecution<T, TDocument> CreateExecution<TDocument>()
        => GetType() == typeof(ParserConverter<U, T>)
            ? Execution<TDocument>.Get(this)
            : base.CreateExecution<TDocument>();

    internal override ParseResult<T, TDocument> ParseDynamicCore<TDocument>(Cursol<TDocument> cursol)
    {
        if (GetType() != typeof(ParserConverter<U, T>))
        {
            return base.ParseDynamicCore(cursol);
        }

        // JSONの再帰getterは毎回、新しい変換定義を返す。定義やgetterの意味は変えず、
        // 一時的な変換実行器の確保と、固定した子の実行器の再構築を避ける。
        // 子は参照が同じ定義だけを共有するため、getterが別の子を返す場合もその定義を使う。
        var parser = ParserExecutionCache<Parser<U>, ParserExecution<U, TDocument>>.Get(
            Parser, static it => it.CreateExecution<TDocument>());
        return ConvertResult(parser.ParseCore(cursol), Converter);
    }

    static ParseResult<T, TDocument> ConvertResult<TDocument>(
        ParseResult<U, TDocument> result, Func<U, T> converter)
        where TDocument : IDocument, allows ref struct
        => result.IsSuccess
            ? ParseResult<T, TDocument>.Success(result.Cursol, converter(result.Parsed))
            : ParseResult<T, TDocument>.Failure(result.Cursol);

    sealed class Execution<TDocument> : ParserExecution<T, TDocument>
        where TDocument : IDocument, allows ref struct
    {
        readonly ParserExecution<U, TDocument> parser;
        readonly Func<U, T> converter;

        Execution(ParserConverter<U, T> definition) : base(definition)
        {
            parser = definition.Parser.CreateExecution<TDocument>();
            converter = definition.Converter;
        }

        internal static Execution<TDocument> Get(ParserConverter<U, T> definition)
            => ParserExecutionCache<ParserConverter<U, T>, Execution<TDocument>>.Get(
                definition, static it => new Execution<TDocument>(it));

        internal override ParseResult<T, TDocument> ParseCore(Cursol<TDocument> cursol)
            => ConvertResult(parser.ParseCore(cursol), converter);
    }
}
