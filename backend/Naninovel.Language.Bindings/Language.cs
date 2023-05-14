using System.Diagnostics.CodeAnalysis;
using DotNetJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Naninovel.Common.Bindings;
using Naninovel.Language;
using Naninovel.Metadata;
using static Naninovel.Common.Bindings.Utilities;

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
}, invokePattern: "(.+)", invokeReplacement: "Naninovel.Common.Bindings.Utilities.Try(() => $1)")]

namespace Naninovel.Language;

public class Language
{
    private static IObserverNotifier<IMetadataObserver> notifier = null!;

    public Language (IObserverNotifier<IMetadataObserver> notifier)
    {
        Language.notifier = notifier;
    }

    [JSInvokable, RequiresUnreferencedCode("DI")]
    public static void Boot () => new ServiceCollection()
        // core services
        .AddSingleton<ILogger, JSLogger>()
        .AddSingleton<IEndpointResolver, EndpointResolver>()
        // language services
        .AddSingleton<ICompletionHandler, CompletionHandler>()
        .AddSingleton<IDefinitionHandler, DefinitionHandler>()
        .AddSingleton<IDocumentHandler, DocumentHandler>()
        .AddSingleton<IFoldingHandler, FoldingHandler>()
        .AddSingleton<IHoverHandler, HoverHandler>()
        .AddSingleton<ISymbolHandler, SymbolHandler>()
        .AddSingleton<ITokenHandler, TokenHandler>()
        .AddSingleton<IDiagnosticPublisher, DiagnosticPublisher.JSDiagnosticPublisher>()
        .AddSingleton<Language>()
        .AddJS()
        // observers
        .AddObserving<IMetadataObserver>()
        // initialization
        .BuildServiceProvider()
        .RegisterObservers()
        .GetAll();

    [JSInvokable]
    public static void UpdateMetadata (Project meta) => notifier.Notify(n => n.HandleMetadataChanged(meta));
}
