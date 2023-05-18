using Naninovel.Metadata;

namespace Naninovel.Language;

internal class DiagnoserFactory : IDiagnoserFactory
{
    private readonly IDocumentRegistry docs;
    private readonly DiagnosticRegistry registry;
    private readonly MetadataProvider metaProvider;

    public DiagnoserFactory (IDocumentRegistry docs, DiagnosticRegistry registry, MetadataProvider metaProvider)
    {
        this.docs = docs;
        this.registry = registry;
        this.metaProvider = metaProvider;
    }

    public IDiagnoser Create (DiagnosticContext context) => context switch {
        DiagnosticContext.Syntax => new SyntaxDiagnoser(docs, registry),
        DiagnosticContext.Semantic => new SemanticDiagnoser(metaProvider, docs, registry),
        _ => new NavigationDiagnoser(metaProvider, docs, registry),
    };
}
