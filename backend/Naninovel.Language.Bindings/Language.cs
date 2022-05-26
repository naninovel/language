using DotNetJS;
using Microsoft.JSInterop;
using Naninovel.Metadata;
using static Naninovel.Common.Bindings.Utilities;

namespace Naninovel.Language.Bindings.Language;

public static partial class Language
{
    private static readonly DocumentRegistry registry = new();
    private static Diagnoser diagnoser = null!;
    private static DocumentHandler document = null!;
    private static CompletionHandler completion = null!;
    private static SymbolHandler symbol = null!;
    private static TokenHandler token = null!;
    private static HoverHandler hover = null!;
    private static FoldingHandler folding = null!;

    [JSInvokable]
    public static void CreateHandlers (Project metadata)
    {
        var provider = new MetadataProvider(metadata);
        diagnoser = new Diagnoser(provider, PublishDiagnostics);
        document = new DocumentHandler(registry, diagnoser);
        completion = new CompletionHandler(provider, registry);
        symbol = new SymbolHandler(provider, registry);
        token = new TokenHandler(registry);
        hover = new HoverHandler(provider, registry);
        folding = new FoldingHandler(registry);
    }

    [JSInvokable] public static void OpenDocument (string uri, string text) => Try(() => document.Open(uri, text));
    [JSInvokable] public static void CloseDocument (string uri) => Try(() => document.Close(uri));
    [JSInvokable] public static void ChangeDocument (string uri, DocumentChange[] changes) => Try(() => document.Change(uri, changes));
    [JSInvokable] public static CompletionItem[] Complete (string uri, Position pos) => Try(() => completion.Complete(uri, pos));
    [JSInvokable] public static Symbol[] GetSymbols (string uri) => Try(() => symbol.GetSymbols(uri));
    [JSInvokable] public static TokenLegend GetTokenLegend () => Try(token.GetTokenLegend);
    [JSInvokable] public static Tokens GetAllTokens (string uri) => Try(() => token.GetAllTokens(uri));
    [JSInvokable] public static Tokens GetTokens (string uri, Range range) => Try(() => token.GetTokens(uri, range));
    [JSInvokable] public static Hover? Hover (string uri, Position pos) => Try(() => hover.Hover(uri, pos));
    [JSInvokable] public static FoldingRange[] GetFoldingRanges (string uri) => Try(() => folding.GetFoldingRanges(uri));

    [JSFunction] public static partial void PublishDiagnostics (string uri, Diagnostic[] diagnostics);
}
