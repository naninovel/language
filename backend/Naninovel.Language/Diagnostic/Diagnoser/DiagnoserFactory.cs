using Naninovel.Metadata;

namespace Naninovel.Language;

internal class DiagnoserFactory(IDocumentRegistry docs, IEndpointRegistry endpoints,
    DiagnosticRegistry registry, IMetadata meta) : IDiagnoserFactory
{
    public IDiagnoser Create (DiagnosticContext context) => context switch {
        DiagnosticContext.Syntax => new SyntaxDiagnoser(docs, registry),
        DiagnosticContext.Semantic => new SemanticDiagnoser(meta, docs, registry),
        _ => new NavigationDiagnoser(meta, docs, endpoints, registry),
    };
}
