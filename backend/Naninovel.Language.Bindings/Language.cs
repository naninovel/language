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
    typeof(ISettingsHandler),
    typeof(IMetadataHandler),
    typeof(IDocumentHandler),
    typeof(ICompletionHandler),
    typeof(IDefinitionHandler),
    typeof(IFoldingHandler),
    typeof(ISymbolHandler),
    typeof(ITokenHandler),
    typeof(IHoverHandler)
}, invokePattern: "(.+)", invokeReplacement: "Naninovel.Bindings.Utilities.Try(() => $1)")]

namespace Naninovel.Language;

public static class Language
{
    [JSInvokable, RequiresUnreferencedCode("DI")]
    public static void BootServer () => new ServiceCollection()
        // core services
        .AddSingleton<ILogger, JSLogger>()
        .AddSingleton<ISettingsHandler, SettingsHandler>()
        .AddSingleton<IMetadataHandler, MetadataHandler>()
        .AddSingleton<IDocumentRegistry, DocumentRegistry>()
        .AddSingleton<IEndpointRegistry, EndpointRegistry>()
        .AddSingleton<Debug>()
        // language services
        .AddSingleton<IDocumentHandler, DocumentHandler>()
        .AddSingleton<IDiagnosticHandler, DiagnosticHandler>()
        .AddSingleton<ICompletionHandler, CompletionHandler>()
        .AddSingleton<IDefinitionHandler, DefinitionHandler>()
        .AddSingleton<IFoldingHandler, FoldingHandler>()
        .AddSingleton<ISymbolHandler, SymbolHandler>()
        .AddSingleton<ITokenHandler, TokenHandler>()
        .AddSingleton<IHoverHandler, HoverHandler>()
        .AddJS()
        // observers
        .AddObserving<ISettingsObserver>()
        .AddObserving<IMetadataObserver>()
        .AddObserving<IDocumentObserver>()
        // initialization
        .BuildServiceProvider()
        .RegisterObservers()
        .GetAll();
}
