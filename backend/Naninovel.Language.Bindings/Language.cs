using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;
using Naninovel.Bindings;
using Naninovel.Language;
using Naninovel.Metadata;

[assembly: JSPreferences(Space = [Space.Pattern, Space.Replacement])]

[assembly: JSImport(
    typeof(IEditPublisher),
    typeof(IDiagnosticPublisher)
)]

[assembly: JSExport(
    typeof(IConfigurator),
    typeof(IMetadataUpdater),
    typeof(IDocumentHandler),
    typeof(ICompletionHandler),
    typeof(IDefinitionHandler),
    typeof(IFoldingHandler),
    typeof(ISymbolHandler),
    typeof(ITokenHandler),
    typeof(IHoverHandler),
    typeof(IRenameHandler),
    typeof(IFormattingHandler)
)]

namespace Naninovel.Language.Bindings;

public static class Language
{
    [JSInvokable]
    public static void BootServer () => new ServiceCollection()
        // domain and utility services
        .AddSingleton<ILogger, JSLogger>()
        .AddSingleton<IMetadata, MetadataProvider>()
        .AddSingleton<IMetadataUpdater, MetadataUpdater>()
        .AddSingleton<IConfigurator, Configurator>()
        .AddSingleton<IDocumentFactory, DocumentFactory>()
        .AddSingleton<IDocumentRegistry, DocumentRegistry>()
        .AddSingleton<IEndpointRegistry, EndpointRegistry>()
        .AddSingleton<IEndpointRenamer, EndpointRenamer>()
        .AddSingleton<Debug>()
        // language services
        .AddSingleton<IDiagnosticManager, DiagnosticManager>()
        // language request handlers
        .AddSingleton<IDocumentHandler, DocumentHandler>()
        .AddSingleton<ICompletionHandler, CompletionHandler>()
        .AddSingleton<IDefinitionHandler, DefinitionHandler>()
        .AddSingleton<IFoldingHandler, FoldingHandler>()
        .AddSingleton<ISymbolHandler, SymbolHandler>()
        .AddSingleton<ITokenHandler, TokenHandler>()
        .AddSingleton<IHoverHandler, HoverHandler>()
        .AddSingleton<IRenameHandler, RenameHandler>()
        .AddSingleton<IFormattingHandler, FormattingHandler>()
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
