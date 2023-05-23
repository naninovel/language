using System.Collections.Generic;
using Naninovel.Metadata;
using static Naninovel.Language.DiagnosticContext;

namespace Naninovel.Language;

public class DiagnosticHandler : IDiagnosticHandler, ISettingsObserver, IDocumentObserver, IMetadataObserver
{
    private readonly List<IDiagnoser> diagnosers = new();
    private readonly DiagnosticRegistry registry = new();
    private readonly MetadataProvider metaProvider = new();
    private readonly IDocumentRegistry docs;
    private readonly IDiagnosticPublisher publisher;
    private readonly IDiagnoserFactory factory;

    public DiagnosticHandler (IDocumentRegistry docs, IEndpointRegistry endpoints,
        IDiagnosticPublisher publisher, IDiagnoserFactory? factory = null)
    {
        this.publisher = publisher;
        this.docs = docs;
        this.factory = factory ?? new DiagnoserFactory(docs, endpoints, registry, metaProvider);
    }

    public void HandleSettingsChanged (Settings settings)
    {
        diagnosers.Clear();
        if (settings.DiagnoseSyntax) diagnosers.Add(factory.Create(Syntax));
        if (settings.DiagnoseSemantics) diagnosers.Add(factory.Create(Semantic));
        if (settings.DiagnoseNavigation) diagnosers.Add(factory.Create(Navigation));
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

    public void HandleDocumentChanging (string uri, LineRange range)
    {
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentChanging(uri, range);
    }

    public void HandleDocumentChanged (string uri, LineRange range)
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
        {
            diagnoser.HandleDocumentRemoved(uri);
            diagnoser.HandleDocumentAdded(uri);
        }
        Publish();
    }

    private void Publish ()
    {
        foreach (var uri in docs.GetAllUris())
            publisher.PublishDiagnostics(uri, registry.CollectDiagnostics(uri));
    }
}
