using System.Diagnostics.CodeAnalysis;
using DotNetJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Naninovel.Bindings;
using Naninovel.Language;
using static Naninovel.Bindings.Utilities;

[assembly: ExcludeFromCodeCoverage]
[assembly: JSNamespace(NamespacePattern, NamespaceReplacement)]
[assembly: JSImport(new[] { typeof(IDiagnosticPublisher) })]
[assembly: JSExport(new[] {
    typeof(ICompletionHandler),
    typeof(IDefinitionHandler),
    typeof(IDocumentHandler),
    typeof(IFoldingHandler),
    typeof(IHoverHandler),
    typeof(ISymbolHandler),
    typeof(ITokenHandler)
}, invokePattern: "(.+)", invokeReplacement: "Naninovel.Bindings.Utilities.Try(() => $1)")]

namespace Naninovel.Language;

public static class Language
{
    [JSInvokable, RequiresUnreferencedCode("DI")]
    public static void Boot () => new ServiceCollection()
        // handlers
        .AddSingleton<IMetadataHandler, MetadataHandler>()
        .AddSingleton<ICompletionHandler, CompletionHandler>()
        .AddSingleton<IDefinitionHandler, DefinitionHandler>()
        .AddSingleton<IDocumentHandler, DocumentHandler>()
        .AddSingleton<IFoldingHandler, FoldingHandler>()
        .AddSingleton<IHoverHandler, HoverHandler>()
        .AddSingleton<ISymbolHandler, SymbolHandler>()
        .AddSingleton<ITokenHandler, TokenHandler>()
        .AddSingleton<IDiagnosticPublisher, DiagnosticPublisher.JSDiagnosticPublisher>()
        .AddJS()
        // observers
        .AddObserving<IMetadataObserver>()
        .AddObserving<IDocumentObserver>()
        // initialization
        .BuildServiceProvider()
        .RegisterObservers()
        .GetAll();
}
