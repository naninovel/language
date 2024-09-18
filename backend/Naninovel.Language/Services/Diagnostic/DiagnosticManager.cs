using Naninovel.Metadata;
using static Naninovel.Language.DiagnosticContext;

namespace Naninovel.Language;

public class DiagnosticManager : IDiagnosticManager, ISettingsObserver, IDocumentObserver, IMetadataObserver
{
    private readonly List<IDiagnoser> diagnosers = [];
    private readonly DiagnosticRegistry registry = new();
    private readonly IDocumentRegistry docs;
    private readonly IDiagnosticPublisher publisher;
    private readonly IDiagnoserFactory factory;
    private TimeSpan debounceDelay = TimeSpan.Zero;
    private DateTime lastPublishTime = DateTime.MinValue;

    public DiagnosticManager (IMetadata meta, IDocumentRegistry docs, IEndpointRegistry endpoints,
        IDiagnosticPublisher publisher, IDiagnoserFactory? factory = null)
    {
        this.publisher = publisher;
        this.docs = docs;
        this.factory = factory ?? new DiagnoserFactory(docs, endpoints, registry, meta);
    }

    public void HandleSettingsChanged (Settings settings)
    {
        debounceDelay = TimeSpan.FromMilliseconds(settings.DebounceDelay);
        diagnosers.Clear();
        if (settings.DiagnoseSyntax) diagnosers.Add(factory.Create(Syntax));
        if (settings.DiagnoseSemantics) diagnosers.Add(factory.Create(Semantic));
        if (settings.DiagnoseNavigation) diagnosers.Add(factory.Create(Navigation));
        ReDiagnoseAll();
    }

    public void HandleDocumentAdded (string uri)
    {
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentAdded(uri);
        PublishDelayed();
    }

    public void HandleDocumentRemoved (string uri)
    {
        foreach (var diagnoser in diagnosers)
            diagnoser.HandleDocumentRemoved(uri);
        PublishDelayed();
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
        PublishDelayed();
    }

    public void HandleMetadataChanged (Project _)
    {
        ReDiagnoseAll();
    }

    private void ReDiagnoseAll ()
    {
        registry.Clear();
        foreach (var uri in docs.GetAllUris())
        foreach (var diagnoser in diagnosers)
        {
            diagnoser.HandleDocumentRemoved(uri);
            diagnoser.HandleDocumentAdded(uri);
        }
        PublishDelayed();
    }

    private async void PublishDelayed ()
    {
        if (debounceDelay == TimeSpan.Zero)
        {
            Publish();
            return;
        }
        var now = DateTime.Now;
        lastPublishTime = now;
        await Task.Delay(debounceDelay);
        if (now == lastPublishTime) Publish();
    }

    private void Publish ()
    {
        foreach (var uri in docs.GetAllUris())
            publisher.PublishDiagnostics(uri, registry.CollectDiagnostics(uri));
    }
}
