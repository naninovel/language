using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;
using Naninovel.Bindings;
using Naninovel.Language;
using Naninovel.Metadata;

[assembly: JSPreferences(Space = [Space.Pattern, Space.Replacement])]
[assembly: JSImport(typeof(IDiagnosticPublisher))]
[assembly: JSExport(
    typeof(ISettingsHandler),
    typeof(IMetadataHandler),
    typeof(IDocumentHandler),
    typeof(ICompletionHandler),
    typeof(IDefinitionHandler),
    typeof(IFoldingHandler),
    typeof(ISymbolHandler),
    typeof(ITokenHandler),
    typeof(IHoverHandler))
]

namespace Naninovel.Language.Bindings;

public static class Language
{
    [JSInvokable]
    public static void BootServer () => new ServiceCollection()
        // core services
        .AddSingleton<ILogger, JSLogger>()
        .AddSingleton<IMetadata, MetadataProvider>()
        .AddSingleton<IMetadataHandler, MetadataHandler>()
        .AddSingleton<ISettingsHandler, SettingsHandler>()
        .AddSingleton<IDocumentFactory, DocumentFactory>()
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
        // observers
        .AddObserving<ISettingsObserver>()
        .AddObserving<IMetadataObserver>()
        .AddObserving<IDocumentObserver>()
        // initialization
        .AddBootsharp()
        .BuildServiceProvider()
        .RegisterObservers()
        .RunBootsharp()
        .GetAll();
}
