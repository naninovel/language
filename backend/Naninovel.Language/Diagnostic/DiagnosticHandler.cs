using System.Collections.Generic;
using Naninovel.Metadata;

namespace Naninovel.Language;

public class DiagnosticHandler : IDiagnosticHandler, ISettingsObserver, IDocumentObserver, IMetadataObserver
{
    private readonly List<Diagnoser> diagnosers = new();
    private readonly DiagnosticRegistry registry = new();
    private readonly MetadataProvider metaProvider = new();
    private readonly IDocumentRegistry docs;
    private readonly IDiagnosticPublisher publisher;

    public DiagnosticHandler (IDocumentRegistry docs, IDiagnosticPublisher publisher)
    {
        this.publisher = publisher;
        this.docs = docs;
    }

    public void HandleSettingsChanged (Settings settings)
    {
        diagnosers.Clear();
        if (settings.DiagnoseSyntax)
            diagnosers.Add(new SyntaxDiagnoser(docs, registry));
        if (settings.DiagnoseSemantics)
            diagnosers.Add(new SemanticDiagnoser(metaProvider, docs, registry));
        if (settings.DiagnoseNavigation)
            diagnosers.Add(new NavigationDiagnoser(metaProvider, docs, registry));
        RediagnoseAll();
    }

    public void HandleDocumentAdded (string uri)
    {
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentAdded(uri);
        Publish();
    }

    public void HandleDocumentRemoved (string uri)
    {
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentRemoved(uri);
        Publish();
    }

    public void HandleDocumentChanged (string uri, in LineRange range)
    {
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentChanged(uri, range);
        Publish();
    }

    public void HandleMetadataChanged (Project meta)
    {
        metaProvider.Update(meta);
        RediagnoseAll();
    }

    private void RediagnoseAll ()
    {
        registry.Clear();
        foreach (var uri in docs.GetAllUris())
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentChanged(uri, new(0, docs.Get(uri).LineCount - 1));
        Publish();
    }

    private void Publish ()
    {
        foreach (var uri in registry.GetAllUris())
            publisher.PublishDiagnostics(uri, registry.GetDiagnostics(uri));
    }
}
