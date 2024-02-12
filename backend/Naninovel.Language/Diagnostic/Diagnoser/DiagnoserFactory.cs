using Naninovel.Metadata;

namespace Naninovel.Language;

internal class DiagnoserFactory(IDocumentRegistry docs, IEndpointRegistry endpoints,
    DiagnosticRegistry registry, MetadataProvider metaProvider) : IDiagnoserFactory
{
    public IDiagnoser Create (DiagnosticContext context) => context switch {
        DiagnosticContext.Syntax => new SyntaxDiagnoser(docs, registry),
        DiagnosticContext.Semantic => new SemanticDiagnoser(metaProvider, docs, registry),
        _ => new NavigationDiagnoser(metaProvider, docs, endpoints, registry),
    };
}
