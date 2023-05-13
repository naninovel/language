using System.Collections.Generic;
using DotNetJS;
using Microsoft.JSInterop;
using Naninovel.Metadata;
using static Naninovel.Common.Bindings.Utilities;

[assembly: JSNamespace(NamespacePattern, NamespaceReplacement)]

namespace Naninovel.Language.Bindings.Language;

public static partial class Language
{
    private static readonly EndpointRegistry endpoints = new();
    private static readonly DocumentRegistry docs = new(endpoints);
    private static Diagnoser diagnoser = null!;
    private static DocumentHandler document = null!;
    private static CompletionHandler completion = null!;
    private static SymbolHandler symbol = null!;
    private static TokenHandler token = null!;
    private static HoverHandler hover = null!;
    private static FoldingHandler folding = null!;
    private static DefinitionHandler definition = null!;

    [JSInvokable]
    public static void CreateHandlers (Project metadata)
    {
        var provider = new MetadataProvider(metadata);
        diagnoser = new Diagnoser(provider, docs, PublishDiagnostics);
        document = new DocumentHandler(docs);
        completion = new CompletionHandler(provider, docs);
        symbol = new SymbolHandler(provider, docs);
        token = new TokenHandler(docs);
        hover = new HoverHandler(provider, docs);
        folding = new FoldingHandler(docs);
        definition = new DefinitionHandler(docs, new EndpointResolver(provider));
    }

    [JSInvokable] public static void OpenDocuments (IReadOnlyList<DocumentInfo> infos) => Try(document.Open, infos);
    [JSInvokable] public static void CloseDocument (string uri) => Try(document.Close, uri);
    [JSInvokable] public static void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes) => Try(document.Change, uri, changes);
    [JSInvokable] public static CompletionItem[] Complete (string uri, Position pos) => Try(completion.Complete, uri, pos);
    [JSInvokable] public static Symbol[] GetSymbols (string uri) => Try(symbol.GetSymbols, uri);
    [JSInvokable] public static TokenLegend GetTokenLegend () => Try(token.GetTokenLegend);
    [JSInvokable] public static Tokens GetAllTokens (string uri) => Try(token.GetAllTokens, uri);
    [JSInvokable] public static Tokens GetTokens (string uri, Range range) => Try(token.GetTokens, uri, range);
    [JSInvokable] public static Hover? Hover (string uri, Position pos) => Try(hover.Hover, uri, pos);
    [JSInvokable] public static FoldingRange[] GetFoldingRanges (string uri) => Try(folding.GetFoldingRanges, uri);
    [JSInvokable] public static LocationLink[]? GotoDefinition (string uri, Position pos) => Try(definition.GotoDefinition, uri, pos);

    [JSFunction] public static partial void PublishDiagnostics (string uri, IReadOnlyList<Diagnostic> diagnostics);
}
