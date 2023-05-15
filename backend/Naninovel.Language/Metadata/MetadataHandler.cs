using Naninovel.Metadata;

namespace Naninovel.Language;

public class MetadataHandler : IMetadataHandler
{
    private readonly IObserverNotifier<IMetadataObserver> notifier;
    private readonly IDocumentRegistry docs;
    private readonly IDiagnoser diagnoser;

    public MetadataHandler (IObserverNotifier<IMetadataObserver> notifier, IDocumentRegistry docs, IDiagnoser diagnoser)
    {
        this.notifier = notifier;
        this.docs = docs;
        this.diagnoser = diagnoser;
    }

    public void UpdateMetadata (Project meta)
    {
        notifier.Notify(n => n.HandleMetadataChanged(meta));
        foreach (var uri in docs.GetAllUris())
            diagnoser.Diagnose(uri);
    }
}
