using System.Collections.Generic;
using Naninovel.Metadata;

namespace Naninovel.Language;

public class DiagnosticHandler : IDiagnosticHandler, IDocumentObserver, IMetadataObserver
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

    public void HandleDocumentAdded (string uri)
    {
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentAdded(uri);
        registry.Publish(publisher);
    }

    public void HandleDocumentRemoved (string uri)
    {
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentRemoved(uri);
        registry.Publish(publisher);
    }

    public void HandleDocumentChanged (string uri, in LineRange range)
    {
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentChanged(uri, range);
        registry.Publish(publisher);
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
        registry.Publish(publisher);
    }
}
